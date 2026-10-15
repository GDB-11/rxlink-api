using System.Collections.Frozen;
using Infrastructure.Core.Models.Catalog;

namespace Infrastructure.Core.Interfaces.Catalog;

/// <summary>
/// Immutable, point-in-time copy of every catalog table, indexed by public code. Includes inactive rows:
/// deactivated items must still resolve for history, and callers decide whether "active" is required.
/// </summary>
public sealed class CatalogSnapshot
{
    public required long Version { get; init; }
    public required int SchemaVersion { get; init; }
    public required DateTimeOffset LoadedAt { get; init; }

    public required FrozenDictionary<Guid, DocumentTypeCatalogRow> DocumentTypes { get; init; }
    public required FrozenDictionary<Guid, SexCatalogRow> Sexes { get; init; }
    public required FrozenDictionary<Guid, SpecialtyCatalogRow> Specialties { get; init; }
    public required FrozenDictionary<Guid, PharmaceuticalFormCatalogRow> PharmaceuticalForms { get; init; }
    public required FrozenDictionary<Guid, AdministrationRouteCatalogRow> AdministrationRoutes { get; init; }
    public required FrozenDictionary<Guid, FrequencyCatalogRow> Frequencies { get; init; }
    public required FrozenDictionary<Guid, AllergyCatalogRow> Allergies { get; init; }
    public required FrozenDictionary<Guid, AllergySeverityCatalogRow> AllergySeverities { get; init; }
    public required FrozenDictionary<Guid, MedicationCatalogRow> Medications { get; init; }
    public required FrozenDictionary<Guid, InsuranceCatalogRow> Insurances { get; init; }

    // Interaction/allergy rules (Phase 11.1). Optional so a catalog without them (or a test) just yields no warnings.
    public FrozenDictionary<Guid, MedicationClassCatalogRow> MedicationClasses { get; init; } =
        FrozenDictionary<Guid, MedicationClassCatalogRow>.Empty;

    /// <summary>Medication code → codes of the (active) classes it belongs to.</summary>
    public FrozenDictionary<Guid, IReadOnlyList<Guid>> ClassesByMedication { get; init; } =
        FrozenDictionary<Guid, IReadOnlyList<Guid>>.Empty;

    /// <summary>Allergy code → codes of the classes the allergy implies avoiding.</summary>
    public FrozenDictionary<Guid, IReadOnlyList<Guid>> ClassesByAllergy { get; init; } =
        FrozenDictionary<Guid, IReadOnlyList<Guid>>.Empty;

    public IReadOnlyList<MedicationInteractionCatalogRow> MedicationInteractions { get; init; } = [];
}
