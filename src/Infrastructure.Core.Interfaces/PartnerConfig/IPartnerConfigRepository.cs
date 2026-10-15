using BindSharp;
using Infrastructure.Core.DTOs.PartnerConfig;

namespace Infrastructure.Core.Interfaces.PartnerConfig;

public interface IPartnerConfigRepository
{
    /// <summary>Returns <c>PartnerConfig.PartnerCode</c> of the partner DB, or <c>null</c> when the row is missing.</summary>
    Task<Result<Guid?, PartnerConfigRepositoryError>> GetPartnerCodeAsync();
}
