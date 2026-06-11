using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Patient.Request;

public sealed record PatientAllergyRequest
{
    [Required(ErrorMessage = "AllergyCode is required.")]
    public required Guid AllergyCode { get; init; }

    [Required(ErrorMessage = "SeverityCode is required.")]
    public required Guid SeverityCode { get; init; }

    public string? Notes { get; init; }
}