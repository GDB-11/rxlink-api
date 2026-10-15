namespace Application.Core.DTOs.Inventory.Response;

public sealed class StockReportItemResponse
{
    public required Guid MedicationCode { get; init; }
    public required string Name { get; init; }
    public required decimal Entries { get; init; }
    public required decimal Returns { get; init; }
    public required decimal Dispensed { get; init; }
    public required decimal Losses { get; init; }
    public required decimal Adjustments { get; init; }
    public required decimal Shortfall { get; init; }

    /// <summary>Current balance (not the balance at the end of the range).</summary>
    public required decimal StockQuantity { get; init; }
}
