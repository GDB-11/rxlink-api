using Application.Core.DTOs.PartnerConfig.Errors;
using Application.Core.DTOs.PartnerConfig.Response;
using Application.Core.Interfaces.PartnerConfig;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Clinic;
using Infrastructure.Core.Models.Catalog;

namespace Application.Core.Services.PartnerConfig;

public sealed class PartnerConfigService : IPartnerConfig
{
    /// <summary>Route of the logo endpoint; the hash is appended as <c>v</c> so a new logo busts caches.</summary>
    private const string LogoPath = "/api/partner/logo";

    private readonly IPartnerRegistryReader _registry;
    private readonly IClinicRepository _clinics;

    public PartnerConfigService(IPartnerRegistryReader registry, IClinicRepository clinics)
    {
        _registry = registry;
        _clinics = clinics;
    }

    /// <inheritdoc/>
    public async Task<Result<PublicPartnerConfigResponse, PartnerConfigError>> GetPublicAsync()
    {
        Result<PartnerIdentityRow, PartnerConfigError> identity = await _registry.GetIdentityAsync()
            .MapErrorAsync(PartnerConfigError (error) => new PartnerConfigUnavailableError(error.Details, error.Exception))
            .EnsureNotNullAsync<PartnerIdentityRow, PartnerConfigError>(new PartnerConfigUnavailableError("Partner is not registered."));

        if (!identity.IsSuccess)
            return Result<PublicPartnerConfigResponse, PartnerConfigError>.Failure(identity.Error!);

        // The logo only contributes its hash; a failed or empty read just means "no logo".
        string? logoHash = (await _registry.GetLogoAsync()).Match(row => row?.Sha256, _ => null);

        // The clinic name is decoration: failing to read it must not break the login screen.
        string? clinicName = (await _clinics.GetDefaultAsync()).Match(row => row?.Name, _ => null);

        PartnerIdentityRow row = identity.Value!;
        return Result<PublicPartnerConfigResponse, PartnerConfigError>.Success(new PublicPartnerConfigResponse
        {
            PartnerCode = row.PartnerCode,
            Slug = row.Slug,
            DisplayName = row.DisplayName,
            LogoUrl = logoHash is null ? null : $"{LogoPath}?v={logoHash}",
            BrandPrimaryColor = row.BrandPrimaryColor,
            BrandAccentColor = row.BrandAccentColor,
            Country = row.Country,
            Timezone = row.Timezone,
            Currency = row.Currency,
            Locale = row.Locale,
            ClinicName = clinicName,
        });
    }

    /// <inheritdoc/>
    public Task<Result<PartnerLogoResponse, PartnerConfigError>> GetLogoAsync() =>
        _registry.GetLogoAsync()
            .MapErrorAsync(PartnerConfigError (error) => new PartnerConfigUnavailableError(error.Details, error.Exception))
            .EnsureNotNullAsync<PartnerLogoRow, PartnerConfigError>(new PartnerLogoNotFoundError())
            .MapAsync(row => new PartnerLogoResponse
            {
                ContentType = row.ContentType,
                Data = row.Data,
                Sha256 = row.Sha256,
            });
}
