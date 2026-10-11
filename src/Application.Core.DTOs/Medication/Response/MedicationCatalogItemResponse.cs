namespace Application.Core.DTOs.Medication.Response;

/// <summary>A shared-catalog medication merged with this partner's settings. Code-based: the catalog has no integer ids.</summary>
public sealed class MedicationCatalogItemResponse
{
    public required Guid MedicationCode { get; init; }
    public required Guid PharmaceuticalFormCode { get; init; }
    public required string PharmaceuticalFormName { get; init; }
    public required Guid AdministrationRouteCode { get; init; }
    public required string AdministrationRouteName { get; init; }
    public required string GenericName { get; init; }
    public string? CommercialName { get; init; }
    public required string Concentration { get; init; }

    /// <summary>Active in the shared catalog (and not soft-deleted).</summary>
    public required bool IsActive { get; init; }

    /// <summary>Enabled by this partner. Items the partner never configured are disabled.</summary>
    public required bool IsEnabled { get; init; }

    public decimal? ReorderLevel { get; init; }

    /// <summary>Default clinic's balance; <c>null</c> when there is no stock row.</summary>
    public decimal? StockQuantity { get; init; }
}
