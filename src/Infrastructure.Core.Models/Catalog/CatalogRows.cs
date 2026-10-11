namespace Infrastructure.Core.Models.Catalog;

/// <summary>Columns shared by the catalog lookups that are just a code and a name.</summary>
public abstract class CatalogNamedRow
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
}

public sealed class DocumentTypeCatalogRow : CatalogNamedRow;

public sealed class SexCatalogRow : CatalogNamedRow;

public sealed class SpecialtyCatalogRow : CatalogNamedRow;

public sealed class PharmaceuticalFormCatalogRow : CatalogNamedRow;

public sealed class AdministrationRouteCatalogRow : CatalogNamedRow;

public sealed class FrequencyCatalogRow
{
    public required Guid Code { get; init; }
    public required string Description { get; init; }
    public required int IntervalHours { get; init; }
    public required bool IsActive { get; init; }
}

public sealed class AllergyCatalogRow
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
}

public sealed class AllergySeverityCatalogRow
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public required short SortOrder { get; init; }
    public required bool IsActive { get; init; }
}

public sealed class MedicationCatalogRow
{
    public required Guid Code { get; init; }
    public required Guid PharmaceuticalFormCode { get; init; }
    public required Guid AdministrationRouteCode { get; init; }
    public required string GenericName { get; init; }
    public string? CommercialName { get; init; }
    public required string Concentration { get; init; }
    public required bool IsActive { get; init; }
    public DateTimeOffset? DeletedAt { get; init; }
}

public sealed class InsuranceCatalogRow
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public required decimal DefaultCoveragePercentage { get; init; }
    public required bool IsActive { get; init; }
}

public sealed class CatalogVersionRow
{
    public required long Version { get; init; }
    public required int SchemaVersion { get; init; }
}
