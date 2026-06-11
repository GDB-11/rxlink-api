using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Prescription.Request;

public sealed record PrescriptionDetailRequest
{
    [Required(ErrorMessage = "MedicationCode is required.")]
    public required Guid MedicationCode { get; init; }

    [Required(ErrorMessage = "AdministrationRouteCode is required.")]
    public required Guid AdministrationRouteCode { get; init; }

    [Required(ErrorMessage = "FrequencyCode is required.")]
    public required Guid FrequencyCode { get; init; }

    [Required(ErrorMessage = "Dose is required.")]
    [MaxLength(100, ErrorMessage = "Dose must not exceed 100 characters.")]
    public required string Dose { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "DurationDays must be at least 1.")]
    public required int DurationDays { get; init; }

    public string? Instructions { get; init; }
}