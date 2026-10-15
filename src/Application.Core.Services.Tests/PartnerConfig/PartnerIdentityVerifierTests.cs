using Application.Core.Interfaces.PartnerConfig;
using Application.Core.Services.PartnerConfig;
using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.DTOs.PartnerConfig;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerConfig;
using Infrastructure.Core.Models.Catalog;
using NSubstitute;

namespace Application.Core.Services.Tests.PartnerConfig;

public sealed class PartnerIdentityVerifierTests
{
    private readonly IPartnerConfigRepository _config = Substitute.For<IPartnerConfigRepository>();
    private readonly IPartnerRegistryReader _registry = Substitute.For<IPartnerRegistryReader>();
    private readonly PartnerIdentityVerifier _sut;

    public PartnerIdentityVerifierTests()
    {
        _sut = new PartnerIdentityVerifier(_config, _registry);
    }

    private void Local(Guid? code) =>
        _config.GetPartnerCodeAsync().Returns(Result<Guid?, PartnerConfigRepositoryError>.Success(code));

    private void Remote(Guid? code) =>
        _registry.GetIdentityAsync().Returns(Result<PartnerIdentityRow?, CatalogRepositoryError>.Success(
            code is null
                ? null
                : new PartnerIdentityRow
                {
                    PartnerCode = code.Value, Slug = "demo", DisplayName = "Demo",
                    Country = "PE", Timezone = "America/Lima", Currency = "PEN", Locale = "es-PE"
                }));

    [Fact]
    public async Task SameCode_IsMatch()
    {
        Guid code = Guid.NewGuid();
        Local(code);
        Remote(code);

        Assert.Equal(PartnerIdentityCheck.Match, await _sut.VerifyAsync());
    }

    [Fact]
    public async Task DifferentCodes_IsMismatch()
    {
        Local(Guid.NewGuid());
        Remote(Guid.NewGuid());

        Assert.Equal(PartnerIdentityCheck.Mismatch, await _sut.VerifyAsync());
    }

    [Fact]
    public async Task CatalogNotRegistered_IsUnverified()
    {
        Local(Guid.NewGuid());
        Remote(null);

        Assert.Equal(PartnerIdentityCheck.Unverified, await _sut.VerifyAsync());
    }

    [Fact]
    public async Task CatalogUnreadable_IsUnverified()
    {
        Local(Guid.NewGuid());
        _registry.GetIdentityAsync().Returns(Result<PartnerIdentityRow?, CatalogRepositoryError>.Failure(
            new CatalogLoadError("boom")));

        Assert.Equal(PartnerIdentityCheck.Unverified, await _sut.VerifyAsync());
    }

    [Fact]
    public async Task PartnerDbUnreadable_IsUnverified()
    {
        _config.GetPartnerCodeAsync().Returns(Result<Guid?, PartnerConfigRepositoryError>.Failure(
            new GetPartnerCodeError("boom")));
        Remote(Guid.NewGuid());

        Assert.Equal(PartnerIdentityCheck.Unverified, await _sut.VerifyAsync());
    }
}
