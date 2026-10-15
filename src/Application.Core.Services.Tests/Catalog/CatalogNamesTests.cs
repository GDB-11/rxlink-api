using System.Collections.Frozen;
using Application.Core.Services.Catalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;
using NSubstitute;

namespace Application.Core.Services.Tests.Catalog;

public sealed class CatalogNamesTests
{
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly Guid _sex = Guid.NewGuid();
    private readonly Guid _docType = Guid.NewGuid();
    private readonly Guid _inactiveSpecialty = Guid.NewGuid();

    private void WithSnapshot() =>
        _store.Current.Returns(new CatalogSnapshot
        {
            Version = 1, SchemaVersion = 1, LoadedAt = DateTimeOffset.UtcNow,
            DocumentTypes = new Dictionary<Guid, DocumentTypeCatalogRow>
                { [_docType] = new() { Code = _docType, Name = "DNI", IsActive = true } }.ToFrozenDictionary(),
            Sexes = new Dictionary<Guid, SexCatalogRow>
                { [_sex] = new() { Code = _sex, Name = "Femenino", IsActive = true } }.ToFrozenDictionary(),
            Specialties = new Dictionary<Guid, SpecialtyCatalogRow>
                { [_inactiveSpecialty] = new() { Code = _inactiveSpecialty, Name = "Antigua", IsActive = false } }
                .ToFrozenDictionary(),
            PharmaceuticalForms = FrozenDictionary<Guid, PharmaceuticalFormCatalogRow>.Empty,
            AdministrationRoutes = FrozenDictionary<Guid, AdministrationRouteCatalogRow>.Empty,
            Frequencies = FrozenDictionary<Guid, FrequencyCatalogRow>.Empty,
            Allergies = FrozenDictionary<Guid, AllergyCatalogRow>.Empty,
            AllergySeverities = FrozenDictionary<Guid, AllergySeverityCatalogRow>.Empty,
            Medications = FrozenDictionary<Guid, MedicationCatalogRow>.Empty,
            Insurances = FrozenDictionary<Guid, InsuranceCatalogRow>.Empty
        });

    [Fact]
    public void Resolves_names_including_inactive_items()
    {
        WithSnapshot();
        var sut = new CatalogNames(_store);

        Assert.Equal("Femenino", sut.Sex(_sex));
        Assert.Equal("DNI", sut.DocumentType(_docType));
        Assert.Equal("Antigua", sut.Specialty(_inactiveSpecialty));
    }

    [Fact]
    public void Unknown_code_or_cold_start_yields_empty_string()
    {
        var cold = new CatalogNames(_store);
        Assert.Equal(string.Empty, cold.Sex(_sex));

        WithSnapshot();
        Assert.Equal(string.Empty, cold.Sex(Guid.NewGuid()));
    }
}
