namespace Application.Core.DTOs.Inventory.Request;

/// <summary>Inclusive UTC day range of the stock report.</summary>
public sealed record StockReportRequest
{
    public required DateOnly From { get; init; }
    public required DateOnly To { get; init; }
}
