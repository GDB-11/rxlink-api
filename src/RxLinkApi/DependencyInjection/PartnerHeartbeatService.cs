using System.Reflection;
using Application.Core.Config;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerConfig;
using Infrastructure.Core.Models.PartnerConfig;

namespace RxLinkApi.DependencyInjection;

/// <summary>
/// Best-effort telemetry to <c>rxlink-catalog-api</c> every <c>CatalogApi:HeartbeatMinutes</c>. A failure is logged
/// once per outage (status or error type only) and never affects the API.
/// </summary>
internal sealed class PartnerHeartbeatService : BackgroundService
{
    private static readonly string ApiVersion =
        (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
         ?? "unknown").Split('+')[0];

    private readonly IPartnerHeartbeatClient _client;
    private readonly ICatalogSnapshotStore _snapshot;
    private readonly CatalogApiConfig _config;
    private readonly ILogger<PartnerHeartbeatService> _logger;

    public PartnerHeartbeatService(
        IPartnerHeartbeatClient client, ICatalogSnapshotStore snapshot, CatalogApiConfig config,
        ILogger<PartnerHeartbeatService> logger)
    {
        _client = client;
        _snapshot = snapshot;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(Math.Max(1, _config.HeartbeatMinutes)));
        bool failing = false;

        do
        {
            var current = _snapshot.Current;
            var result = await _client.SendAsync(new HeartbeatPayload
            {
                ApiVersion = ApiVersion,
                SupportedCatalogSchemas = new SchemaRange(_config.SupportedCatalogSchemaMin, _config.SupportedCatalogSchemaMax),
                CatalogSchemaVersionSeen = current?.SchemaVersion,
                CatalogVersionSeen = current?.Version
            }, stoppingToken);

            if (result.IsSuccess && failing)
                _logger.LogInformation("Heartbeat to the catalog platform recovered");
            else if (!result.IsSuccess && !failing)
                _logger.LogWarning("Heartbeat to the catalog platform failed: {Reason}", result.Error!.Details);

            failing = !result.IsSuccess;
        } while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); }
        catch (OperationCanceledException) { return false; }
    }
}
