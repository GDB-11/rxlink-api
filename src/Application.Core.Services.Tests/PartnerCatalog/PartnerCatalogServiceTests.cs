using System.Collections.Frozen;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.PartnerCatalog.Errors;
using Application.Core.DTOs.PartnerCatalog.Request;
using Application.Core.Services.PartnerCatalog;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.PartnerCatalog;
using NSubstitute;

namespace Application.Core.Services.Tests.PartnerCatalog;

public sealed class PartnerCatalogServiceTests
{
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly IPartnerCatalogRepository _repository = Substitute.For<IPartnerCatalogRepository>();
    private readonly PartnerCatalogService _sut;

    private readonly Guid _activeSpecialty = Guid.NewGuid();
    private readonly Guid _inactiveSpecialty = Guid.NewGuid();
    private readonly Guid _insurance = Guid.NewGuid();
    private readonly Guid _medication = Guid.NewGuid();

    public PartnerCatalogServiceTests()
    {
        _sut = new PartnerCatalogService(_store, _repository);
        _store.Current.Returns(Snapshot());
    }

    private CatalogSnapshot Snapshot() => new()
    {
        Version = 1, SchemaVersion = 1, LoadedAt = DateTimeOffset.UtcNow,
        DocumentTypes = FrozenDictionary<Guid, DocumentTypeCatalogRow>.Empty,
        Sexes = FrozenDictionary<Guid, SexCatalogRow>.Empty,
        Specialties = new Dictionary<Guid, SpecialtyCatalogRow>
        {
            [_activeSpecialty] = new() { Code = _activeSpecialty, Name = "Cardiología", IsActive = true },
            [_inactiveSpecialty] = new() { Code = _inactiveSpecialty, Name = "Vieja", IsActive = false }
        }.ToFrozenDictionary(),
        PharmaceuticalForms = FrozenDictionary<Guid, PharmaceuticalFormCatalogRow>.Empty,
        AdministrationRoutes = FrozenDictionary<Guid, AdministrationRouteCatalogRow>.Empty,
        Frequencies = FrozenDictionary<Guid, FrequencyCatalogRow>.Empty,
        Allergies = FrozenDictionary<Guid, AllergyCatalogRow>.Empty,
        AllergySeverities = FrozenDictionary<Guid, AllergySeverityCatalogRow>.Empty,
        Medications = new Dictionary<Guid, MedicationCatalogRow>
        {
            [_medication] = new()
            {
                Code = _medication, PharmaceuticalFormCode = Guid.NewGuid(), AdministrationRouteCode = Guid.NewGuid(),
                GenericName = "Paracetamol", Concentration = "500 mg", IsActive = true
            }
        }.ToFrozenDictionary(),
        Insurances = new Dictionary<Guid, InsuranceCatalogRow>
        {
            [_insurance] = new() { Code = _insurance, Name = "Seguro", DefaultCoveragePercentage = 80, IsActive = true }
        }.ToFrozenDictionary()
    };

    private static Result<T, PartnerCatalogRepositoryError> Ok<T>(T value) =>
        Result<T, PartnerCatalogRepositoryError>.Success(value);

    [Fact]
    public async Task UpdateSpecialty_UnknownCode_ReturnsNotFound()
    {
        var result = await _sut.UpdateSpecialtyAsync(Guid.NewGuid(), new UpdatePartnerSpecialtyRequest { IsEnabled = true });

        var rejected = Assert.IsType<PartnerCatalogItemRejectedError>(result.Error);
        Assert.IsType<CatalogItemNotFoundError>(rejected.Reason);
    }

    [Fact]
    public async Task UpdateSpecialty_EnablingInactiveCatalogItem_IsRejected()
    {
        var result = await _sut.UpdateSpecialtyAsync(_inactiveSpecialty, new UpdatePartnerSpecialtyRequest { IsEnabled = true });

        var rejected = Assert.IsType<PartnerCatalogItemRejectedError>(result.Error);
        Assert.IsType<CatalogItemInactiveError>(rejected.Reason);
    }

    [Fact]
    public async Task UpdateSpecialty_DisablingInactiveCatalogItem_IsAllowed()
    {
        var row = new PartnerSpecialtyRow { SpecialtyCode = _inactiveSpecialty, IsEnabled = true, PriceInPerson = 50, PriceVirtual = 40 };
        _repository.GetSpecialtyAsync(_inactiveSpecialty).Returns(Ok(row));
        _repository.SaveSpecialtyAsync(_inactiveSpecialty, false, 50, 40)
            .Returns(Ok(new PartnerSpecialtyRow { SpecialtyCode = _inactiveSpecialty, IsEnabled = false, PriceInPerson = 50, PriceVirtual = 40 }));

        var result = await _sut.UpdateSpecialtyAsync(_inactiveSpecialty, new UpdatePartnerSpecialtyRequest { IsEnabled = false });

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsEnabled);
    }

    [Fact]
    public async Task UpdateSpecialty_EnablingWithoutPrices_BreaksRule()
    {
        _repository.GetSpecialtyAsync(_activeSpecialty)
            .Returns(Ok(new PartnerSpecialtyRow { SpecialtyCode = _activeSpecialty, IsEnabled = false, PriceInPerson = 0, PriceVirtual = 0 }));

        var result = await _sut.UpdateSpecialtyAsync(_activeSpecialty, new UpdatePartnerSpecialtyRequest { IsEnabled = true, PriceInPerson = 80 });

        Assert.IsType<PartnerCatalogRuleError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().SaveSpecialtyAsync(default, default, default, default);
    }

    [Fact]
    public async Task UpdateSpecialty_MergesOmittedFieldsWithCurrentValues()
    {
        _repository.GetSpecialtyAsync(_activeSpecialty)
            .Returns(Ok(new PartnerSpecialtyRow { SpecialtyCode = _activeSpecialty, IsEnabled = true, PriceInPerson = 80, PriceVirtual = 60 }));
        _repository.SaveSpecialtyAsync(_activeSpecialty, true, 90, 60)
            .Returns(Ok(new PartnerSpecialtyRow { SpecialtyCode = _activeSpecialty, IsEnabled = true, PriceInPerson = 90, PriceVirtual = 60 }));

        var result = await _sut.UpdateSpecialtyAsync(_activeSpecialty, new UpdatePartnerSpecialtyRequest { PriceInPerson = 90 });

        Assert.True(result.IsSuccess);
        Assert.Equal(90, result.Value.PriceInPerson);
    }

    [Fact]
    public async Task UpdateMedication_NoSnapshot_ReturnsUnavailable()
    {
        _store.Current.Returns((CatalogSnapshot?)null);

        var result = await _sut.UpdateMedicationAsync(_medication, new UpdatePartnerMedicationRequest { IsEnabled = true });

        var rejected = Assert.IsType<PartnerCatalogItemRejectedError>(result.Error);
        Assert.IsType<CatalogValidationUnavailableError>(rejected.Reason);
    }

    [Fact]
    public async Task UpdateMedication_PassesReorderLevelAndKeepsEnabledFlag()
    {
        _repository.GetMedicationAsync(_medication)
            .Returns(Ok(new PartnerMedicationRow { MedicationCode = _medication, IsEnabled = true }));
        _repository.SaveMedicationAsync(_medication, true, 10m)
            .Returns(Ok(new PartnerMedicationRow { MedicationCode = _medication, IsEnabled = true, ReorderLevel = 10m }));

        var result = await _sut.UpdateMedicationAsync(_medication, new UpdatePartnerMedicationRequest { ReorderLevel = 10m });

        Assert.True(result.IsSuccess);
        Assert.Equal(10m, result.Value.ReorderLevel);
    }

    [Fact]
    public async Task UpdateInsurance_ClearAndValueTogether_BreaksRule()
    {
        var result = await _sut.UpdateInsuranceAsync(_insurance,
            new UpdatePartnerInsuranceRequest { CoveragePercentageOverride = 50, ClearCoverageOverride = true });

        Assert.IsType<PartnerCatalogRuleError>(result.Error);
    }

    [Fact]
    public async Task UpdateInsurance_Clear_SavesNullOverride()
    {
        _repository.GetInsuranceAsync(_insurance)
            .Returns(Ok(new PartnerInsuranceRow { InsuranceCode = _insurance, IsEnabled = true, CoveragePercentageOverride = 50 }));
        _repository.SaveInsuranceAsync(_insurance, true, null)
            .Returns(Ok(new PartnerInsuranceRow { InsuranceCode = _insurance, IsEnabled = true }));

        var result = await _sut.UpdateInsuranceAsync(_insurance, new UpdatePartnerInsuranceRequest { ClearCoverageOverride = true });

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CoveragePercentageOverride);
    }
}
