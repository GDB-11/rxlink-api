using Application.Core.DTOs.PartnerConfig.Errors;
using Application.Core.DTOs.PartnerConfig.Response;
using BindSharp;

namespace Application.Core.Interfaces.PartnerConfig;

public interface IPartnerConfig
{
    /// <summary>Registry identity of this deployment plus its default clinic's name.</summary>
    Task<Result<PublicPartnerConfigResponse, PartnerConfigError>> GetPublicAsync();

    /// <summary>The partner's logo from the registry.</summary>
    Task<Result<PartnerLogoResponse, PartnerConfigError>> GetLogoAsync();
}
