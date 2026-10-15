namespace Infrastructure.Core.Models.Inventory;

public sealed class StockMovementRow
{
    public required Guid StockMovementCode { get; init; }
    public required string MovementType { get; init; }

    /// <summary>+1 adds to the balance, -1 takes from it.</summary>
    public required short Direction { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? ShortfallQuantity { get; init; }
    public string? BatchNumber { get; init; }
    public DateTime? ExpiresOn { get; init; }
    public string? Reason { get; init; }
    public string? Username { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}

public sealed class StockMovementPageRow
{
    public required IReadOnlyList<StockMovementRow> Items { get; init; }
    public required int TotalCount { get; init; }
}

/// <summary>Ledger totals of one medication over a period, in the default clinic.</summary>
public sealed class StockReportRow
{
    public required Guid MedicationCode { get; init; }
    public required decimal Entries { get; init; }
    public required decimal Returns { get; init; }
    public required decimal Dispensed { get; init; }
    public required decimal Losses { get; init; }

    /// <summary>Signed sum of the adjustments (increases minus decreases).</summary>
    public required decimal Adjustments { get; init; }

    /// <summary>Quantity dispensed through a stock override that the balance did not cover.</summary>
    public required decimal Shortfall { get; init; }
}
