using System.Collections.Frozen;
using Application.Core.DTOs.Lookup.Errors;
using Application.Core.Services.Lookup;
using BindSharp;
using Infrastructure.Core.DTOs.Lookup;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.Lookup;
using Infrastructure.Core.Models.PartnerCatalog;
using NSubstitute;

namespace Application.Core.Services.Tests.Lookup;

public sealed class LookupServiceTests
{
    private readonly ILookupRepository _repository = Substitute.For<ILookupRepository>();
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly IPartnerCatalogRepository _partner = Substitute.For<IPartnerCatalogRepository>();
    private readonly LookupService _sut;

    private readonly Guid _route = Guid.NewGuid();
    private readonly Guid _enabledMed = Guid.NewGuid();
    private readonly Guid _disabledMed = Guid.NewGuid();
    private readonly Guid _enabledSpecialty = Guid.NewGuid();
    private readonly Guid _disabledSpecialty = Guid.NewGuid();
    private readonly Guid _insurance = Guid.NewGuid();

    public LookupServiceTests()
    {
        _store.Current.Returns(BuildSnapshot());
        _sut = new LookupService(_repository, _store, _partner);

        _partner.GetAllMedicationsAsync().Returns(Ok<IReadOnlyList<PartnerMedicationRow>>(
        [
            new() { MedicationCode = _enabledMed, IsEnabled = true },
            new() { MedicationCode = _disabledMed, IsEnabled = false }
        ]));
        _partner.GetAllSpecialtiesAsync().Returns(Ok<IReadOnlyList<PartnerSpecialtyRow>>(
        [
            new() { SpecialtyCode = _enabledSpecialty, IsEnabled = true, PriceInPerson = 80, PriceVirtual = 60 },
            new() { SpecialtyCode = _disabledSpecialty, IsEnabled = false, PriceInPerson = 1, PriceVirtual = 1 }
        ]));
        _partner.GetAllInsurancesAsync().Returns(Ok<IReadOnlyList<PartnerInsuranceRow>>(
        [
            new() { InsuranceCode = _insurance, IsEnabled = true, CoveragePercentageOverride = 55 }
        ]));
        _repository.GetActiveRolesAsync().Returns(Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>.Success(
            [new GuidLookupRow { Code = Guid.NewGuid(), Name = "Doctor" }]));
        _repository.GetActivePrescriptionStatusesAsync().Returns(Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>.Success([]));
        _repository.GetActiveConsultationTypesAsync().Returns(Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>.Success([]));
    }

    private CatalogSnapshot BuildSnapshot() => new()
    {
        Version = 1,
        SchemaVersion = 1,
        LoadedAt = DateTimeOffset.UtcNow,
        DocumentTypes = new Dictionary<Guid, DocumentTypeCatalogRow>
        {
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Name = "DNI", IsActive = true },
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Name = "Viejo", IsActive = false }
        }.ToFrozenDictionary(),
        Sexes = new Dictionary<Guid, SexCatalogRow>
        {
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Name = "Masculino", IsActive = true }
        }.ToFrozenDictionary(),
        Specialties = new Dictionary<Guid, SpecialtyCatalogRow>
        {
            [_enabledSpecialty] = new() { Code = _enabledSpecialty, Name = "Cardiología", IsActive = true },
            [_disabledSpecialty] = new() { Code = _disabledSpecialty, Name = "Pediatría", IsActive = true }
        }.ToFrozenDictionary(),
        PharmaceuticalForms = FrozenDictionary<Guid, PharmaceuticalFormCatalogRow>.Empty,
        AdministrationRoutes = new Dictionary<Guid, AdministrationRouteCatalogRow>
        {
            [_route] = new() { Code = _route, Name = "Oral", IsActive = true }
        }.ToFrozenDictionary(),
        Frequencies = new Dictionary<Guid, FrequencyCatalogRow>
        {
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Description = "Cada 12 h", IntervalHours = 12, IsActive = true },
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Description = "Cada 8 h", IntervalHours = 8, IsActive = true }
        }.ToFrozenDictionary(),
        Allergies = FrozenDictionary<Guid, AllergyCatalogRow>.Empty,
        AllergySeverities = new Dictionary<Guid, AllergySeverityCatalogRow>
        {
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Name = "Grave", SortOrder = 3, IsActive = true },
            [Guid.NewGuid()] = new() { Code = Guid.NewGuid(), Name = "Leve", SortOrder = 1, IsActive = true }
        }.ToFrozenDictionary(),
        Medications = new Dictionary<Guid, MedicationCatalogRow>
        {
            [_enabledMed] = new()
            {
                Code = _enabledMed, PharmaceuticalFormCode = Guid.NewGuid(), AdministrationRouteCode = _route,
                GenericName = "Paracetamol", CommercialName = "Panadol", Concentration = "500 mg", IsActive = true
            },
            [_disabledMed] = new()
            {
                Code = _disabledMed, PharmaceuticalFormCode = Guid.NewGuid(), AdministrationRouteCode = _route,
                GenericName = "Amoxicilina", Concentration = "1 g", IsActive = true
            }
        }.ToFrozenDictionary(),
        Insurances = new Dictionary<Guid, InsuranceCatalogRow>
        {
            [_insurance] = new() { Code = _insurance, Name = "Pacífico", DefaultCoveragePercentage = 80, IsActive = true }
        }.ToFrozenDictionary()
    };

    private static Result<T, PartnerCatalogRepositoryError> Ok<T>(T value) =>
        Result<T, PartnerCatalogRepositoryError>.Success(value);

    [Fact]
    public async Task PrescriptionLookups_OnlyEnabledMedications_WithCommercialNameLabel()
    {
        var result = await _sut.GetPrescriptionLookupsAsync();

        Assert.True(result.IsSuccess);
        var medication = Assert.Single(result.Value.Medications);
        Assert.Equal(_enabledMed, medication.Code);
        Assert.Equal("Panadol - Paracetamol", medication.Name);
        Assert.Equal("500 mg", medication.DefaultDose);
        Assert.Equal(_route, medication.DefaultAdministrationRouteCode);
        Assert.Equal(["Cada 8 h", "Cada 12 h"], result.Value.Frequencies.Select(f => f.Name));
    }

    [Fact]
    public async Task AppointmentLookups_UseEnabledSpecialtiesAndEffectiveCoverage()
    {
        var result = await _sut.GetAppointmentLookupsAsync();

        var specialty = Assert.Single(result.Value.Specialties);
        Assert.Equal(_enabledSpecialty, specialty.Code);
        Assert.Equal(80, specialty.PriceInPerson);
        Assert.Equal(55, Assert.Single(result.Value.Insurances).CoveragePercentage);
    }

    [Fact]
    public async Task UserLookups_ListActiveCatalogItems_AndEnabledSpecialtiesOnly()
    {
        var result = await _sut.GetUserLookupsAsync();

        Assert.Equal(["DNI"], result.Value.DocumentTypes.Select(d => d.Name));
        Assert.Equal(["Cardiología"], result.Value.Specialties.Select(s => s.Name));
        Assert.Equal(["Doctor"], result.Value.Roles.Select(r => r.Name));
    }

    [Fact]
    public async Task PatientLookups_OrderSeveritiesBySortOrder()
    {
        var result = await _sut.GetPatientLookupsAsync();

        Assert.Equal(["Leve", "Grave"], result.Value.AllergySeverities.Select(s => s.Name));
    }

    [Fact]
    public async Task NoSnapshot_EveryCatalogLookupIsUnavailable()
    {
        _store.Current.Returns((CatalogSnapshot?)null);

        Assert.IsType<LookupCatalogUnavailableError>((await _sut.GetUserLookupsAsync()).Error);
        Assert.IsType<LookupCatalogUnavailableError>((await _sut.GetPatientLookupsAsync()).Error);
        Assert.IsType<LookupCatalogUnavailableError>((await _sut.GetPrescriptionLookupsAsync()).Error);
        Assert.IsType<LookupCatalogUnavailableError>((await _sut.GetAppointmentLookupsAsync()).Error);
    }
}
