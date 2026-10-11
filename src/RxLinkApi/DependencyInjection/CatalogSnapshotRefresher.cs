using Application.Core.Config;
using Infrastructure.Core.Interfaces.Catalog;

namespace RxLinkApi.DependencyInjection;

/// <summary>
/// Loads the catalog snapshot at startup, then polls <c>CatalogVersion</c> every <c>Catalog:RefreshSeconds</c> and
/// reloads only when it changed. Failures never stop the API: the last good snapshot keeps being served, and with no
/// snapshot the load is retried on every tick.
/// </summary>
internal sealed class CatalogSnapshotRefresher : BackgroundService
{
    private readonly ICatalogSnapshotStore _store;
    private readonly CatalogConfig _config;
    private readonly ILogger<CatalogSnapshotRefresher> _logger;

    public CatalogSnapshotRefresher(
        ICatalogSnapshotStore store, CatalogConfig config, ILogger<CatalogSnapshotRefresher> logger)
    {
        _store = store;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(1, _config.RefreshSeconds));

        using PeriodicTimer timer = new(interval);
        bool failing = false;

        do
        {
            long? previous = _store.Current?.Version;
            var result = await _store.RefreshIfChangedAsync();

            if (result.IsSuccess)
            {
                if (previous != result.Value.Version)
                    _logger.LogInformation(
                        "Catalog snapshot loaded: version {Version}, schema {SchemaVersion}, {Medications} medications",
                        result.Value.Version, result.Value.SchemaVersion, result.Value.Medications.Count);
                else if (failing)
                    _logger.LogInformation("Catalog connection recovered (version {Version})", result.Value.Version);

                failing = false;
            }
            else if (!failing)
            {
                // Log once per outage, error type only: message/details are exception text from the data layer.
                failing = true;
                _logger.LogError(
                    "Catalog refresh failed ({ErrorType}); {State}", result.Error.GetType().Name,
                    _store.Current is null ? "no snapshot loaded, catalog-dependent writes are rejected"
                        : "serving the last loaded snapshot");
            }
        } while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); }
        catch (OperationCanceledException) { return false; }
    }
}
