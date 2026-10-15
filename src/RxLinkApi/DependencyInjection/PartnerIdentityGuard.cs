using Application.Core.Interfaces.PartnerConfig;

namespace RxLinkApi.DependencyInjection;

/// <summary>
/// Refuses to start when the partner DB and the catalog credential belong to different partners (§10.1).
/// Only a definite mismatch stops the API: an unreachable or unregistered catalog must not (it degrades readiness
/// instead, see <c>CatalogHealthCheck</c>), so that case is logged and the API starts.
/// </summary>
internal sealed class PartnerIdentityGuard : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PartnerIdentityGuard> _logger;

    public PartnerIdentityGuard(IServiceScopeFactory scopes, ILogger<PartnerIdentityGuard> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopes.CreateScope();
        PartnerIdentityCheck check = await scope.ServiceProvider
            .GetRequiredService<IPartnerIdentityVerifier>().VerifyAsync();

        switch (check)
        {
            case PartnerIdentityCheck.Match:
                _logger.LogInformation("Partner identity verified: partner DB and catalog registry agree");
                break;
            case PartnerIdentityCheck.Unverified:
                _logger.LogWarning(
                    "Partner identity could not be verified (partner DB or catalog registry unreadable, or not registered)");
                break;
            default:
                _logger.LogCritical(
                    "PartnerConfig.PartnerCode does not match the catalog registry: wrong database or catalog credential");
                throw new InvalidOperationException(
                    "Partner identity mismatch: this API's database and its catalog credential belong to different partners.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
