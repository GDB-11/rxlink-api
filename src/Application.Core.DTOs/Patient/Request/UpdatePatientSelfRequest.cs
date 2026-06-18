using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Patient.Request;

public sealed class UpdatePatientSelfRequest
{
    [Required]
    [MaxLength(30)]
    public required string Phone { get; init; }

    [MaxLength(30)]
    public string? AlternativePhone { get; init; }

    public string? Address { get; init; }

    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(30)]
    public string? EmergencyContactPhone { get; init; }
}
