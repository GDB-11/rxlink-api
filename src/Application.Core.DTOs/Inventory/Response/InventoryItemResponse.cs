namespace Application.Core.DTOs.Inventory.Response;

/// <summary>An enabled medication with its balance in the default clinic.</summary>
public sealed class InventoryItemResponse
{
    public required Guid MedicationCode { get; init; }
    public required string Name { get; init; }
    public required string Concentration { get; init; }
    public required string PharmaceuticalFormName { get; init; }
    public required decimal StockQuantity { get; init; }
    public decimal? ReorderLevel { get; init; }

    /// <summary><see cref="StockStatus"/>: <c>InStock</c>, <c>Low</c> or <c>Out</c>.</summary>
    public required string Status { get; init; }
}
