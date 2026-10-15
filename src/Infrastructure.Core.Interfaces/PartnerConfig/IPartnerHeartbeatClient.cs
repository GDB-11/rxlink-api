using BindSharp;
using Infrastructure.Core.DTOs.PartnerConfig;
using Infrastructure.Core.Models.PartnerConfig;

namespace Infrastructure.Core.Interfaces.PartnerConfig;

/// <summary>Calls <c>POST /api/v1/partners/me/heartbeat</c> on <c>rxlink-catalog-api</c> with the partner API key.</summary>
public interface IPartnerHeartbeatClient
{
    Task<Result<Unit, PartnerConfigRepositoryError>> SendAsync(HeartbeatPayload payload, CancellationToken cancellationToken);
}
