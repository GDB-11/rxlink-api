using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Prescription.Request;

/// <summary>The quantity the nurse hands over for every line of the prescription.</summary>
public sealed record DispensePrescriptionRequest
{
    [Required(ErrorMessage = "Items is required.")]
    [MinLength(1, ErrorMessage = "At least one line must be dispensed.")]
    public required IReadOnlyList<DispenseLineRequest> Items { get; init; }

    /// <summary>
    /// Required to dispense more than the clinic's balance (stock override, plan D14); the shortfall is recorded
    /// on the movement and the balance goes negative until reconciled. Ignored when stock covers the quantities.
    /// </summary>
    [MaxLength(500, ErrorMessage = "OverrideReason must be at most 500 characters.")]
    public string? OverrideReason { get; init; }
}

public sealed record DispenseLineRequest
{
    [Required(ErrorMessage = "PrescriptionDetailCode is required.")]
    public required Guid PrescriptionDetailCode { get; init; }

    [Range(typeof(decimal), "0.001", "999999999", ErrorMessage = "Quantity must be greater than zero.")]
    public required decimal Quantity { get; init; }
}
