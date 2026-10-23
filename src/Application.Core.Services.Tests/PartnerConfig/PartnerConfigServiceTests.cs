using Application.Core.Config;
using Application.Core.DTOs.PartnerConfig.Errors;
using Application.Core.DTOs.PartnerConfig.Response;
using Application.Core.Services.PartnerConfig;
using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.DTOs.Clinic;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Clinic;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.Clinic;
using NSubstitute;

namespace Application.Core.Services.Tests.PartnerConfig;

public sealed class PartnerConfigServiceTests
{
    private readonly IPartnerRegistryReader _registry = Substitute.For<IPartnerRegistryReader>();
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly PartnerConfigService _sut;

    private static readonly PartnerIdentityRow Identity = new()
    {
        PartnerCode = Guid.NewGuid(),
        Slug = "clinica-demo",
        DisplayName = "Clínica Demo",
        Country = "PE",
        Timezone = "America/Lima",
        Currency = "PEN",
        Locale = "es-PE",
        BrandPrimaryColor = "#112233",
    };

    public PartnerConfigServiceTests()
    {
        _sut = new PartnerConfigService(_registry, _clinics, new MobileClientConfig { MinimumAppVersion = "1.2.0" });
        _registry.GetIdentityAsync().Returns(Result<PartnerIdentityRow?, CatalogRepositoryError>.Success(Identity));
        _registry.GetLogoAsync().Returns(Result<PartnerLogoRow?, CatalogRepositoryError>.Success(null));
        _clinics.GetDefaultAsync().Returns(Result<ClinicRow?, ClinicRepositoryError>.Success(null));
    }

    [Fact]
    public async Task GetPublic_WithLogoAndClinic_ReturnsVersionedLogoUrlAndClinicName()
    {
        _registry.GetLogoAsync().Returns(Result<PartnerLogoRow?, CatalogRepositoryError>.Success(
            new PartnerLogoRow { ContentType = "image/png", Data = [1, 2], Sha256 = "abc123" }));
        _clinics.GetDefaultAsync().Returns(Result<ClinicRow?, ClinicRepositoryError>.Success(
            new ClinicRow { ClinicCode = Guid.NewGuid(), Name = "Sede Principal", UpdatedAt = DateTimeOffset.UtcNow }));

        Result<PublicPartnerConfigResponse, PartnerConfigError> result = await _sut.GetPublicAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("/api/v1/partner/logo?v=abc123", result.Value!.LogoUrl);
        Assert.Equal("Sede Principal", result.Value.ClinicName);
        Assert.Equal(Identity.PartnerCode, result.Value.PartnerCode);
        Assert.Equal("#112233", result.Value.BrandPrimaryColor);
        Assert.Equal("1.2.0", result.Value.MinimumAppVersion);
        Assert.Empty(result.Value.Features);
    }

    [Fact]
    public async Task GetPublic_WithoutLogoOrClinic_StillSucceeds()
    {
        Result<PublicPartnerConfigResponse, PartnerConfigError> result = await _sut.GetPublicAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.LogoUrl);
        Assert.Null(result.Value.ClinicName);
    }

    [Fact]
    public async Task GetPublic_WhenLogoAndClinicReadsFail_StillSucceeds()
    {
        _registry.GetLogoAsync().Returns(Result<PartnerLogoRow?, CatalogRepositoryError>.Failure(new CatalogLoadError("x")));
        _clinics.GetDefaultAsync().Returns(Result<ClinicRow?, ClinicRepositoryError>.Failure(new GetClinicError("x")));

        Result<PublicPartnerConfigResponse, PartnerConfigError> result = await _sut.GetPublicAsync();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.LogoUrl);
        Assert.Null(result.Value.ClinicName);
    }

    [Fact]
    public async Task GetPublic_WhenNotRegistered_IsUnavailable()
    {
        _registry.GetIdentityAsync().Returns(Result<PartnerIdentityRow?, CatalogRepositoryError>.Success(null));

        Result<PublicPartnerConfigResponse, PartnerConfigError> result = await _sut.GetPublicAsync();

        Assert.IsType<PartnerConfigUnavailableError>(result.Error);
    }

    [Fact]
    public async Task GetPublic_WhenRegistryFails_IsUnavailable()
    {
        _registry.GetIdentityAsync().Returns(Result<PartnerIdentityRow?, CatalogRepositoryError>.Failure(new CatalogLoadError("down")));

        Result<PublicPartnerConfigResponse, PartnerConfigError> result = await _sut.GetPublicAsync();

        Assert.IsType<PartnerConfigUnavailableError>(result.Error);
    }

    [Fact]
    public async Task GetLogo_WhenMissing_IsNotFound()
    {
        Result<PartnerLogoResponse, PartnerConfigError> result = await _sut.GetLogoAsync();

        Assert.IsType<PartnerLogoNotFoundError>(result.Error);
    }

    [Fact]
    public async Task GetLogo_ReturnsBytesAndHash()
    {
        _registry.GetLogoAsync().Returns(Result<PartnerLogoRow?, CatalogRepositoryError>.Success(
            new PartnerLogoRow { ContentType = "image/webp", Data = [9], Sha256 = "h" }));

        Result<PartnerLogoResponse, PartnerConfigError> result = await _sut.GetLogoAsync();

        Assert.Equal("image/webp", result.Value!.ContentType);
        Assert.Equal("h", result.Value.Sha256);
    }
}
