using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Prescription.Request;

public sealed record UpdatePrescriptionRequest
{
    public string? Notes { get; init; }

    [Required(ErrorMessage = "ValidUntil is required.")]
    public required DateOnly ValidUntil { get; init; }

    [Required(ErrorMessage = "At least one detail is required.")]
    [MinLength(1, ErrorMessage = "At least one detail is required.")]
    public required IReadOnlyList<PrescriptionDetailRequest> Details { get; init; }
}