namespace Application.Core.DTOs.Inventory.Response;

public sealed class LowStockItemResponse
{
    public required Guid MedicationCode { get; init; }
    public required string Name { get; init; }
    public required decimal StockQuantity { get; init; }
    public required decimal ReorderLevel { get; init; }

    /// <summary><see cref="StockStatus.Low"/> or <see cref="StockStatus.Out"/>.</summary>
    public required string Status { get; init; }
}
