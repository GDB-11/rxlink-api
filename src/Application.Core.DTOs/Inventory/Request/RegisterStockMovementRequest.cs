using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Inventory.Request;

/// <summary>Manual stock movement. <c>Entry</c> adds, <c>Loss</c> takes, <c>Adjustment</c> goes either way (<see cref="Direction"/>).</summary>
public sealed record RegisterStockMovementRequest
{
    /// <summary><c>Entry</c>, <c>Adjustment</c> or <c>Loss</c> (dispenses come from the dispensing flow).</summary>
    [Required]
    public required string MovementType { get; init; }

    /// <summary>Only for <c>Adjustment</c>: +1 increases the balance, -1 decreases it.</summary>
    public int? Direction { get; init; }

    public required decimal Quantity { get; init; }

    [MaxLength(50, ErrorMessage = "BatchNumber must not exceed 50 characters.")]
    public string? BatchNumber { get; init; }

    public DateOnly? ExpiresOn { get; init; }

    /// <summary>Mandatory for <c>Adjustment</c> and <c>Loss</c>.</summary>
    [MaxLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
    public string? Reason { get; init; }
}
