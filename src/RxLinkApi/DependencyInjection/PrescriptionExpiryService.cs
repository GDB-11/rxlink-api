using Infrastructure.Core.Interfaces.Prescription;
using Infrastructure.Core.Services.Activity;
using Infrastructure.Core.Services.Prescription;
using RxLinkApi.Http;
using System.Data;

namespace RxLinkApi.DependencyInjection;

/// <summary>
/// Applies the time-based prescription transitions (Activo/Suspendido → Caducado, Dispensado → Finalizado) every
/// <c>PrescriptionExpiry:IntervalMinutes</c>. Idempotent, so running on several instances is safe. A failure is logged
/// once per outage (error type only) and the next tick retries.
/// </summary>
internal sealed class PrescriptionExpiryService : BackgroundService
{
    internal const string JobName = "prescription-expiry";
    private const int BatchSize = 500;

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeSpan _interval;
    private readonly ILogger<PrescriptionExpiryService> _logger;

    public PrescriptionExpiryService(IServiceScopeFactory scopes, IConfiguration configuration,
        ILogger<PrescriptionExpiryService> logger)
    {
        _scopes = scopes;
        _interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("PrescriptionExpiry:IntervalMinutes", 60)));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(_interval);
        bool failing = false;

        do
        {
            try
            {
                int changed = await SweepAsync();
                if (changed > 0)
                    _logger.LogInformation("Prescription expiry sweep updated {Count} prescriptions", changed);
                if (failing)
                    _logger.LogInformation("Prescription expiry sweep recovered");
                failing = false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (!failing)
                    _logger.LogError("Prescription expiry sweep failed: {ErrorType}", ex.GetType().Name);
                failing = true;
            }
        } while (await WaitAsync(timer, stoppingToken));
    }

    /// <summary>Runs batches until nothing is due. Returns how many prescriptions changed status.</summary>
    internal async Task<int> SweepAsync()
    {
        using IServiceScope scope = _scopes.CreateScope();
        SystemUserContext context = new(JobName);
        IDbConnection connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
        IPrescriptionExpiryRepository repository = new PrescriptionExpiryRepository(
            connection, context, new ActivityLogger(connection, context));

        int total = 0;
        int batch;
        do
        {
            batch = (await repository.ExpireBatchAsync(BatchSize)).Count;
            total += batch;
        } while (batch == BatchSize);

        return total;
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); }
        catch (OperationCanceledException) { return false; }
    }
}
