using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Patient.Request;

public sealed record UpdatePatientRequest
{
    [Required(ErrorMessage = "MedicalRecordNumber is required.")]
    [MaxLength(50, ErrorMessage = "MedicalRecordNumber must not exceed 50 characters.")]
    public required string MedicalRecordNumber { get; init; }
}