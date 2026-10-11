using System.Collections.Frozen;
using System.Text.Json;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Prescription.Request;
using Application.Core.Services.Prescription;
using BindSharp;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;
using NSubstitute;

namespace Application.Core.Services.Tests.Prescription;

public sealed class PrescriptionLineSnapshotBuilderTests
{
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly PrescriptionLineSnapshotBuilder _sut;

    private readonly Guid _form = Guid.NewGuid();
    private readonly Guid _route = Guid.NewGuid();
    private readonly Guid _frequency = Guid.NewGuid();
    private readonly Guid _medication = Guid.NewGuid();

    public PrescriptionLineSnapshotBuilderTests()
    {
        _store.Current.Returns(new CatalogSnapshot
        {
            Version = 1,
            SchemaVersion = 1,
            LoadedAt = DateTimeOffset.UtcNow,
            DocumentTypes = FrozenDictionary<Guid, DocumentTypeCatalogRow>.Empty,
            Sexes = FrozenDictionary<Guid, SexCatalogRow>.Empty,
            Specialties = FrozenDictionary<Guid, SpecialtyCatalogRow>.Empty,
            PharmaceuticalForms = new Dictionary<Guid, PharmaceuticalFormCatalogRow>
                { [_form] = new() { Code = _form, Name = "Tableta", IsActive = true } }.ToFrozenDictionary(),
            AdministrationRoutes = new Dictionary<Guid, AdministrationRouteCatalogRow>
                { [_route] = new() { Code = _route, Name = "Oral", IsActive = true } }.ToFrozenDictionary(),
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
                    Code = _medication, PharmaceuticalFormCode = _form, AdministrationRouteCode = _route,
                    GenericName = "Paracetamol", CommercialName = null, Concentration = "500 mg", IsActive = true
                }
            }.ToFrozenDictionary(),
            Insurances = FrozenDictionary<Guid, InsuranceCatalogRow>.Empty
        });
        _sut = new PrescriptionLineSnapshotBuilder(_store);
    }

    private PrescriptionDetailRequest Line(Guid? medication = null) => new()
    {
        MedicationCode = medication ?? _medication, AdministrationRouteCode = _route, FrequencyCode = _frequency,
        Dose = "1 tableta", DurationDays = 5, Instructions = "Con agua"
    };

    [Fact]
    public void BuildJson_AttachesCatalogValuesAsPrescribed()
    {
        Result<string, CatalogValidationError> result = _sut.BuildJson([Line()]);

        Assert.True(result.IsSuccess);
        JsonElement line = JsonDocument.Parse(result.Value).RootElement[0];
        Assert.Equal(_medication, line.GetProperty("MedicationCode").GetGuid());
        Assert.Equal("1 tableta", line.GetProperty("Dose").GetString());
        Assert.Equal("Paracetamol", line.GetProperty("MedicationGenericName").GetString());
        Assert.Equal(JsonValueKind.Null, line.GetProperty("MedicationCommercialName").ValueKind);
        Assert.Equal("500 mg", line.GetProperty("MedicationConcentration").GetString());
        Assert.Equal("Tableta", line.GetProperty("PharmaceuticalFormName").GetString());
        Assert.Equal("Oral", line.GetProperty("AdministrationRouteName").GetString());
        Assert.Equal("Cada 8 h", line.GetProperty("FrequencyDescription").GetString());
        Assert.Equal(8, line.GetProperty("FrequencyIntervalHours").GetInt32());
    }

    [Fact]
    public void BuildJson_UnknownMedication_FailsNotFound()
    {
        Result<string, CatalogValidationError> result = _sut.BuildJson([Line(Guid.NewGuid())]);

        Assert.False(result.IsSuccess);
        Assert.IsType<CatalogItemNotFoundError>(result.Error);
    }

    [Fact]
    public void BuildJson_NoSnapshot_FailsUnavailable()
    {
        _store.Current.Returns((CatalogSnapshot?)null);

        Result<string, CatalogValidationError> result = _sut.BuildJson([Line()]);

        Assert.False(result.IsSuccess);
        Assert.IsType<CatalogValidationUnavailableError>(result.Error);
    }
}
