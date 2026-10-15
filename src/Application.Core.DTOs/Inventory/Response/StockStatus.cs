namespace Application.Core.DTOs.Inventory.Response;

/// <summary>How a medication's balance reads for the prescriber and the stock alerts (plan §11.3).</summary>
public static class StockStatus
{
    public const string InStock = "InStock";
    public const string Low = "Low";
    public const string Out = "Out";

    /// <summary>No balance row or a balance of zero or less is <c>Out</c>; at or below the reorder level is <c>Low</c>.</summary>
    public static string From(decimal? quantity, decimal? reorderLevel) =>
        (quantity ?? 0) <= 0 ? Out
        : reorderLevel is { } level && quantity <= level ? Low
        : InStock;
}
