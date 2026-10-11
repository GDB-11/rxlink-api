using Infrastructure.Core.Interfaces.Catalog;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RxLinkApi.DependencyInjection;

/// <summary>Readiness of the shared catalog: Healthy, or Degraded (cold start, or serving a possibly stale snapshot).</summary>
internal sealed class CatalogHealthCheck : IHealthCheck
{
    private readonly ICatalogSnapshotStore _store;

    public CatalogHealthCheck(ICatalogSnapshotStore store) => _store = store;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = _store.Current;

        HealthCheckResult result = (snapshot, _store.LastFailureAt) switch
        {
            (null, _) => HealthCheckResult.Degraded("Catalog snapshot not loaded"),
            (_, not null) => HealthCheckResult.Degraded("Catalog unreachable, serving the last loaded snapshot"),
            _ => HealthCheckResult.Healthy($"Catalog version {snapshot.Version}")
        };

        return Task.FromResult(result);
    }
}
