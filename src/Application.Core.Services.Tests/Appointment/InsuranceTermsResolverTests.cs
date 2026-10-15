using System.Collections.Frozen;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.Interfaces.Catalog;
using Application.Core.Services.Appointment;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.PartnerCatalog;
using NSubstitute;

namespace Application.Core.Services.Tests.Appointment;

public sealed class InsuranceTermsResolverTests
{
    private readonly ICatalogWriteValidator _validator = Substitute.For<ICatalogWriteValidator>();
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly IPartnerCatalogRepository _partner = Substitute.For<IPartnerCatalogRepository>();
    private readonly InsuranceTermsResolver _sut;

    private readonly Guid _withOverride = Guid.NewGuid();
    private readonly Guid _withoutOverride = Guid.NewGuid();

    public InsuranceTermsResolverTests()
    {
        _validator.ValidateInsuranceAsync(Arg.Any<Guid?>())
            .Returns(Task.FromResult(Result<Unit, CatalogValidationError>.Success(Unit.Value)));
        _store.Current.Returns(new CatalogSnapshot
        {
            Version = 1,
            SchemaVersion = 1,
            LoadedAt = DateTimeOffset.UtcNow,
            DocumentTypes = FrozenDictionary<Guid, DocumentTypeCatalogRow>.Empty,
            Sexes = FrozenDictionary<Guid, SexCatalogRow>.Empty,
            Specialties = FrozenDictionary<Guid, SpecialtyCatalogRow>.Empty,
            PharmaceuticalForms = FrozenDictionary<Guid, PharmaceuticalFormCatalogRow>.Empty,
            AdministrationRoutes = FrozenDictionary<Guid, AdministrationRouteCatalogRow>.Empty,
            Frequencies = FrozenDictionary<Guid, FrequencyCatalogRow>.Empty,
            Allergies = FrozenDictionary<Guid, AllergyCatalogRow>.Empty,
            AllergySeverities = FrozenDictionary<Guid, AllergySeverityCatalogRow>.Empty,
            Medications = FrozenDictionary<Guid, MedicationCatalogRow>.Empty,
            Insurances = new Dictionary<Guid, InsuranceCatalogRow>
            {
                [_withOverride] = new() { Code = _withOverride, Name = "Rímac", DefaultCoveragePercentage = 70, IsActive = true },
                [_withoutOverride] = new() { Code = _withoutOverride, Name = "Pacífico", DefaultCoveragePercentage = 80, IsActive = true }
            }.ToFrozenDictionary()
        });
        _partner.GetInsuranceAsync(_withOverride).Returns(Setting(_withOverride, 55m));
        _partner.GetInsuranceAsync(_withoutOverride).Returns(Setting(_withoutOverride, null));
        _sut = new InsuranceTermsResolver(_validator, _store, _partner);
    }

    private static Task<Result<PartnerInsuranceRow, PartnerCatalogRepositoryError>> Setting(Guid code, decimal? coverageOverride) =>
        Task.FromResult(Result<PartnerInsuranceRow, PartnerCatalogRepositoryError>.Success(
            new PartnerInsuranceRow { InsuranceCode = code, IsEnabled = true, CoveragePercentageOverride = coverageOverride }));

    [Fact]
    public async Task ResolveAsync_NoInsurance_ReturnsNullTermsWithoutValidating()
    {
        var result = await _sut.ResolveAsync(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        await _validator.DidNotReceiveWithAnyArgs().ValidateInsuranceAsync(default);
    }

    [Fact]
    public async Task ResolveAsync_PartnerOverride_UsesOverrideCoverageAndCatalogName()
    {
        var result = await _sut.ResolveAsync(_withOverride);

        Assert.True(result.IsSuccess);
        Assert.Equal(55m, result.Value!.CoveragePercentage);
        Assert.Equal("Rímac", result.Value.InsuranceName);
        Assert.Equal(_withOverride, result.Value.InsuranceCode);
    }

    [Fact]
    public async Task ResolveAsync_NoOverride_UsesCatalogDefaultCoverage()
    {
        var result = await _sut.ResolveAsync(_withoutOverride);

        Assert.True(result.IsSuccess);
        Assert.Equal(80m, result.Value!.CoveragePercentage);
        Assert.Equal("Pacífico", result.Value.InsuranceName);
    }

    [Fact]
    public async Task ResolveAsync_ValidationFails_ReturnsTheValidationError()
    {
        _validator.ValidateInsuranceAsync(_withOverride)
            .Returns(Task.FromResult(Result<Unit, CatalogValidationError>.Failure(
                new CatalogItemNotEnabledError(CatalogItemKind.Insurance))));

        var result = await _sut.ResolveAsync(_withOverride);

        Assert.True(result.IsFailure);
        Assert.IsType<CatalogItemNotEnabledError>(result.Error);
    }

    [Fact]
    public async Task ResolveAsync_PartnerSettingReadFails_ReturnsDataAccessError()
    {
        _partner.GetInsuranceAsync(_withOverride).Returns(Task.FromResult(
            Result<PartnerInsuranceRow, PartnerCatalogRepositoryError>.Failure(new GetPartnerCatalogSettingError())));

        var result = await _sut.ResolveAsync(_withOverride);

        Assert.True(result.IsFailure);
        Assert.IsType<CatalogValidationDataAccessError>(result.Error);
    }
}
