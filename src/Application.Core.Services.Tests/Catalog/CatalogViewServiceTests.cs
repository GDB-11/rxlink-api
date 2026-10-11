using System.Collections.Frozen;
using Application.Core.DTOs.Allergy.Request;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Insurance.Request;
using Application.Core.DTOs.Medication.Request;
using Application.Core.DTOs.Specialty.Request;
using Application.Core.Services.Catalog;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.PartnerCatalog;
using NSubstitute;

namespace Application.Core.Services.Tests.Catalog;

public sealed class CatalogViewServiceTests
{
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly IPartnerCatalogRepository _partner = Substitute.For<IPartnerCatalogRepository>();
    private readonly CatalogViewService _sut;

    private readonly Guid _form = Guid.NewGuid();
    private readonly Guid _route = Guid.NewGuid();
    private readonly Guid _enabledMed = Guid.NewGuid();
    private readonly Guid _disabledMed = Guid.NewGuid();
    private readonly Guid _unconfiguredMed = Guid.NewGuid();
    private readonly Guid _deletedMed = Guid.NewGuid();
    private readonly Guid _enabledSpecialty = Guid.NewGuid();
    private readonly Guid _inactiveSpecialty = Guid.NewGuid();
    private readonly Guid _insurance = Guid.NewGuid();
    private readonly Guid _insuranceWithOverride = Guid.NewGuid();

    public CatalogViewServiceTests()
    {
        _store.Current.Returns(BuildSnapshot());
        _sut = new CatalogViewService(_store, _partner);

        _partner.GetAllMedicationsAsync().Returns(Ok<IReadOnlyList<PartnerMedicationRow>>(
        [
            new() { MedicationCode = _enabledMed, IsEnabled = true, ReorderLevel = 10, StockQuantity = 4 },
            new() { MedicationCode = _disabledMed, IsEnabled = false },
            new() { MedicationCode = _deletedMed, IsEnabled = true }
        ]));
        _partner.GetAllSpecialtiesAsync().Returns(Ok<IReadOnlyList<PartnerSpecialtyRow>>(
        [
            new() { SpecialtyCode = _enabledSpecialty, IsEnabled = true, PriceInPerson = 80, PriceVirtual = 60 },
            new() { SpecialtyCode = _inactiveSpecialty, IsEnabled = true, PriceInPerson = 10, PriceVirtual = 10 }
        ]));
        _partner.GetAllInsurancesAsync().Returns(Ok<IReadOnlyList<PartnerInsuranceRow>>(
        [
            new() { InsuranceCode = _insurance, IsEnabled = true },
            new() { InsuranceCode = _insuranceWithOverride, IsEnabled = true, CoveragePercentageOverride = 55 }
        ]));
    }

    private CatalogSnapshot BuildSnapshot() => new()
    {
        Version = 1,
        SchemaVersion = 1,
        LoadedAt = DateTimeOffset.UtcNow,
        DocumentTypes = FrozenDictionary<Guid, DocumentTypeCatalogRow>.Empty,
        Sexes = FrozenDictionary<Guid, SexCatalogRow>.Empty,
        Specialties = new Dictionary<Guid, SpecialtyCatalogRow>
        {
            [_enabledSpecialty] = new() { Code = _enabledSpecialty, Name = "Cardiología", IsActive = true },
            [_inactiveSpecialty] = new() { Code = _inactiveSpecialty, Name = "Vieja", IsActive = false }
        }.ToFrozenDictionary(),
        PharmaceuticalForms = new Dictionary<Guid, PharmaceuticalFormCatalogRow>
        {
            [_form] = new() { Code = _form, Name = "Tableta", IsActive = true }
        }.ToFrozenDictionary(),
        AdministrationRoutes = new Dictionary<Guid, AdministrationRouteCatalogRow>
        {
            [_route] = new() { Code = _route, Name = "Oral", IsActive = true }
        }.ToFrozenDictionary(),
        Frequencies = FrozenDictionary<Guid, FrequencyCatalogRow>.Empty,
        Allergies = new Dictionary<Guid, AllergyCatalogRow>
        {
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Name = "Penicilina", IsActive = true },
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Name = "Látex", IsActive = false }
        }.ToFrozenDictionary(),
        AllergySeverities = FrozenDictionary<Guid, AllergySeverityCatalogRow>.Empty,
        Medications = new Dictionary<Guid, MedicationCatalogRow>
        {
            [_enabledMed] = Medication(_enabledMed, "Paracetamol", "Panadol"),
            [_disabledMed] = Medication(_disabledMed, "Amoxicilina", null),
            [_unconfiguredMed] = Medication(_unconfiguredMed, "Ibuprofeno", null),
            [_deletedMed] = Medication(_deletedMed, "Retirado", null, deleted: true)
        }.ToFrozenDictionary(),
        Insurances = new Dictionary<Guid, InsuranceCatalogRow>
        {
            [_insurance] = new() { Code = _insurance, Name = "Pacífico", DefaultCoveragePercentage = 80, IsActive = true },
            [_insuranceWithOverride] = new() { Code = _insuranceWithOverride, Name = "Rímac", DefaultCoveragePercentage = 70, IsActive = true }
        }.ToFrozenDictionary()
    };

    private MedicationCatalogRow Medication(Guid code, string generic, string? commercial, bool deleted = false) => new()
    {
        Code = code, PharmaceuticalFormCode = _form, AdministrationRouteCode = _route,
        GenericName = generic, CommercialName = commercial, Concentration = "500 mg",
        IsActive = !deleted, DeletedAt = deleted ? DateTimeOffset.UtcNow : null
    };

    private static Result<T, PartnerCatalogRepositoryError> Ok<T>(T value) =>
        Result<T, PartnerCatalogRepositoryError>.Success(value);

    [Fact]
    public async Task Medications_MergesPartnerSettings_AndTreatsMissingRowAsDisabled()
    {
        var result = await _sut.GetMedicationsPageAsync(new MedicationPageRequest());

        Assert.True(result.IsSuccess);
        var items = result.Value.Items;
        Assert.Equal(4, result.Value.TotalCount);
        Assert.Equal(["Amoxicilina", "Ibuprofeno", "Paracetamol", "Retirado"], items.Select(i => i.GenericName));

        var enabled = items.Single(i => i.MedicationCode == _enabledMed);
        Assert.True(enabled.IsEnabled);
        Assert.Equal(10, enabled.ReorderLevel);
        Assert.Equal(4, enabled.StockQuantity);
        Assert.Equal("Tableta", enabled.PharmaceuticalFormName);
        Assert.Equal("Oral", enabled.AdministrationRouteName);

        Assert.False(items.Single(i => i.MedicationCode == _unconfiguredMed).IsEnabled);
        Assert.False(items.Single(i => i.MedicationCode == _deletedMed).IsActive);
    }

    [Fact]
    public async Task Medications_EnabledOnly_ExcludesDisabledAndSoftDeleted()
    {
        var result = await _sut.GetMedicationsPageAsync(new MedicationPageRequest { EnabledOnly = true });

        Assert.Equal([_enabledMed], result.Value.Items.Select(i => i.MedicationCode));
    }

    [Fact]
    public async Task Medications_SearchMatchesGenericOrCommercialNameIgnoringCase()
    {
        var byCommercial = await _sut.GetMedicationsPageAsync(new MedicationPageRequest { Search = "pana" });
        var byGeneric = await _sut.GetMedicationsPageAsync(new MedicationPageRequest { Search = "AMOXI" });

        Assert.Equal([_enabledMed], byCommercial.Value.Items.Select(i => i.MedicationCode));
        Assert.Equal([_disabledMed], byGeneric.Value.Items.Select(i => i.MedicationCode));
    }

    [Fact]
    public async Task Medications_PagesInMemory()
    {
        var result = await _sut.GetMedicationsPageAsync(new MedicationPageRequest { Page = 2, PageSize = 3 });

        Assert.Equal(4, result.Value.TotalCount);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.Page);
    }

    [Fact]
    public async Task Medications_InvalidPaging_IsClamped()
    {
        var result = await _sut.GetMedicationsPageAsync(new MedicationPageRequest { Page = 0, PageSize = 0 });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.PageSize);
    }

    [Fact]
    public async Task Specialties_ReturnsPartnerPrices_AndZeroWhenNoRow()
    {
        var result = await _sut.GetSpecialtiesPageAsync(new SpecialtyPageRequest());

        var cardio = result.Value.Items.Single(i => i.SpecialtyCode == _enabledSpecialty);
        Assert.True(cardio.IsEnabled);
        Assert.Equal(80, cardio.PriceInPerson);
        Assert.Equal(60, cardio.PriceVirtual);
    }

    [Fact]
    public async Task Specialties_EnabledOnly_ExcludesDeactivatedCatalogItems()
    {
        var result = await _sut.GetSpecialtiesPageAsync(new SpecialtyPageRequest { EnabledOnly = true });

        Assert.Equal([_enabledSpecialty], result.Value.Items.Select(i => i.SpecialtyCode));
    }

    [Fact]
    public async Task Insurances_EffectiveCoverageUsesOverrideOtherwiseDefault()
    {
        var result = await _sut.GetInsurancesPageAsync(new InsurancePageRequest());

        var plain = result.Value.Items.Single(i => i.InsuranceCode == _insurance);
        var overridden = result.Value.Items.Single(i => i.InsuranceCode == _insuranceWithOverride);
        Assert.Equal(80, plain.CoveragePercentage);
        Assert.Null(plain.CoveragePercentageOverride);
        Assert.Equal(55, overridden.CoveragePercentage);
        Assert.Equal(70, overridden.DefaultCoveragePercentage);
    }

    [Fact]
    public async Task Allergies_ListsCatalogIncludingInactive_Searchable()
    {
        var all = await _sut.GetAllergiesPageAsync(new AllergyPageRequest());
        var search = await _sut.GetAllergiesPageAsync(new AllergyPageRequest { Search = "peni" });

        Assert.Equal(2, all.Value.TotalCount);
        Assert.Single(search.Value.Items);
    }

    [Fact]
    public async Task NoSnapshot_ReturnsUnavailable_WithoutTouchingPartnerDb()
    {
        _store.Current.Returns((CatalogSnapshot?)null);

        var result = await _sut.GetMedicationsPageAsync(new MedicationPageRequest());

        Assert.False(result.IsSuccess);
        Assert.IsType<CatalogReadUnavailableError>(result.Error);
        await _partner.DidNotReceive().GetAllMedicationsAsync();
    }

    [Fact]
    public async Task PartnerReadFailure_MapsToDataAccessError()
    {
        _partner.GetAllInsurancesAsync().Returns(
            Result<IReadOnlyList<PartnerInsuranceRow>, PartnerCatalogRepositoryError>.Failure(
                new GetPartnerCatalogSettingError("boom")));

        var result = await _sut.GetInsurancesPageAsync(new InsurancePageRequest());

        Assert.IsType<CatalogReadDataAccessError>(result.Error);
    }
}
