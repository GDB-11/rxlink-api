namespace Application.Core.DTOs.Inventory.Response;

public sealed class StockMovementResponse
{
    public required Guid StockMovementCode { get; init; }
    public required string MovementType { get; init; }
    public required int Direction { get; init; }
    public required decimal Quantity { get; init; }
    public decimal? ShortfallQuantity { get; init; }
    public string? BatchNumber { get; init; }
    public DateOnly? ExpiresOn { get; init; }
    public string? Reason { get; init; }
    public string? Username { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}
