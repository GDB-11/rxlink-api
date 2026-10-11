using System.Collections.Frozen;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.Services.Catalog;
using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;
using NSubstitute;

namespace Application.Core.Services.Tests.Catalog;

public sealed class CatalogWriteValidatorTests
{
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly IPartnerEnablementRepository _enablement = Substitute.For<IPartnerEnablementRepository>();
    private readonly CatalogWriteValidator _sut;

    private readonly Guid _medication = Guid.NewGuid();
    private readonly Guid _route = Guid.NewGuid();
    private readonly Guid _frequency = Guid.NewGuid();
    private readonly Guid _specialty = Guid.NewGuid();
    private readonly Guid _insurance = Guid.NewGuid();
    private readonly Guid _inactive = Guid.NewGuid();

    public CatalogWriteValidatorTests()
    {
        _store.Current.Returns(BuildSnapshot());
        _sut = new CatalogWriteValidator(_store, _enablement);
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
            [_specialty] = new() { Code = _specialty, Name = "Cardiología", IsActive = true },
            [_inactive] = new() { Code = _inactive, Name = "Vieja", IsActive = false }
        }.ToFrozenDictionary(),
        PharmaceuticalForms = FrozenDictionary<Guid, PharmaceuticalFormCatalogRow>.Empty,
        AdministrationRoutes = new Dictionary<Guid, AdministrationRouteCatalogRow>
        {
            [_route] = new() { Code = _route, Name = "Oral", IsActive = true }
        }.ToFrozenDictionary(),
        Frequencies = new Dictionary<Guid, FrequencyCatalogRow>
        {
            [_frequency] = new() { Code = _frequency, Description = "Cada 8 h", IntervalHours = 8, IsActive = true }
        }.ToFrozenDictionary(),
        Allergies = FrozenDictionary<Guid, AllergyCatalogRow>.Empty,
        AllergySeverities = FrozenDictionary<Guid, AllergySeverityCatalogRow>.Empty,
        Medications = new Dictionary<Guid, MedicationCatalogRow>
        {
            [_medication] = new()
            {
                Code = _medication, PharmaceuticalFormCode = Guid.NewGuid(), AdministrationRouteCode = _route,
                GenericName = "Paracetamol", Concentration = "500 mg", IsActive = true
            },
            [_inactive] = new()
            {
                Code = _inactive, PharmaceuticalFormCode = Guid.NewGuid(), AdministrationRouteCode = _route,
                GenericName = "Retirado", Concentration = "1 g", IsActive = false
            }
        }.ToFrozenDictionary(),
        Insurances = new Dictionary<Guid, InsuranceCatalogRow>
        {
            [_insurance] = new() { Code = _insurance, Name = "Pacífico", DefaultCoveragePercentage = 80, IsActive = true }
        }.ToFrozenDictionary()
    };

    private void Enabled(PartnerEnablementKind kind, params Guid[] codes) =>
        _enablement.GetEnabledAsync(kind, Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(Result<IReadOnlySet<Guid>, CatalogRepositoryError>.Success(codes.ToHashSet()));

    [Fact]
    public async Task PrescriptionLines_ActiveEnabledMedication_Passes()
    {
        Enabled(PartnerEnablementKind.Medication, _medication);

        var result = await _sut.ValidatePrescriptionLinesAsync([(_medication, _route, _frequency)]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task PrescriptionLines_MedicationNotEnabledByPartner_IsRejected()
    {
        Enabled(PartnerEnablementKind.Medication);

        var result = await _sut.ValidatePrescriptionLinesAsync([(_medication, _route, _frequency)]);

        var error = Assert.IsType<CatalogItemNotEnabledError>(result.Error);
        Assert.Equal(CatalogItemKind.Medication, error.Kind);
    }

    [Fact]
    public async Task PrescriptionLines_DeactivatedMedication_IsRejected()
    {
        var result = await _sut.ValidatePrescriptionLinesAsync([(_inactive, _route, _frequency)]);

        Assert.IsType<CatalogItemInactiveError>(result.Error);
        await _enablement.DidNotReceiveWithAnyArgs().GetEnabledAsync(default, default!);
    }

    [Fact]
    public async Task PrescriptionLines_UnknownRoute_IsRejected()
    {
        var result = await _sut.ValidatePrescriptionLinesAsync([(_medication, Guid.NewGuid(), _frequency)]);

        var error = Assert.IsType<CatalogItemNotFoundError>(result.Error);
        Assert.Equal(CatalogItemKind.AdministrationRoute, error.Kind);
    }

    [Fact]
    public async Task PrescriptionLines_EnablementReadFails_ReturnsDataAccessError()
    {
        _enablement.GetEnabledAsync(Arg.Any<PartnerEnablementKind>(), Arg.Any<IReadOnlyCollection<Guid>>())
            .Returns(Result<IReadOnlySet<Guid>, CatalogRepositoryError>.Failure(new PartnerEnablementReadError()));

        var result = await _sut.ValidatePrescriptionLinesAsync([(_medication, _route, _frequency)]);

        Assert.IsType<CatalogValidationDataAccessError>(result.Error);
    }

    [Fact]
    public async Task AnyValidation_WithoutSnapshot_ReturnsUnavailable()
    {
        _store.Current.Returns((CatalogSnapshot?)null);

        var result = await _sut.ValidatePrescriptionLinesAsync([(_medication, _route, _frequency)]);

        Assert.IsType<CatalogValidationUnavailableError>(result.Error);
    }

    [Fact]
    public async Task Specialty_Null_PassesWithoutTouchingTheCatalog()
    {
        _store.Current.Returns((CatalogSnapshot?)null);

        var result = await _sut.ValidateSpecialtyAsync(null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Specialty_EnabledAndActive_Passes()
    {
        Enabled(PartnerEnablementKind.Specialty, _specialty);

        Assert.True((await _sut.ValidateSpecialtyAsync(_specialty)).IsSuccess);
    }

    [Fact]
    public async Task Specialty_NotEnabled_IsRejected()
    {
        Enabled(PartnerEnablementKind.Specialty);

        var result = await _sut.ValidateSpecialtyAsync(_specialty);

        Assert.IsType<CatalogItemNotEnabledError>(result.Error);
    }

    [Fact]
    public async Task Insurance_NotEnabled_IsRejected()
    {
        Enabled(PartnerEnablementKind.Insurance);

        var result = await _sut.ValidateInsuranceAsync(_insurance);

        Assert.IsType<CatalogItemNotEnabledError>(result.Error);
    }

    [Fact]
    public async Task Insurance_Unknown_IsRejected()
    {
        var result = await _sut.ValidateInsuranceAsync(Guid.NewGuid());

        Assert.IsType<CatalogItemNotFoundError>(result.Error);
    }
}
