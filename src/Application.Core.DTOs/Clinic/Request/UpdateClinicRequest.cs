using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Clinic.Request;

/// <summary>
/// Operational fields of the clinic. Identity (name, logo, branding) is owned by the partner registry
/// and is intentionally not editable here. Blank optional fields clear the stored value.
/// </summary>
public sealed record UpdateClinicRequest
{
    [MaxLength(250, ErrorMessage = "Address must not exceed 250 characters.")]
    public string? Address { get; init; }

    [MaxLength(100, ErrorMessage = "District must not exceed 100 characters.")]
    public string? District { get; init; }

    [MaxLength(100, ErrorMessage = "Province must not exceed 100 characters.")]
    public string? Province { get; init; }

    [MaxLength(100, ErrorMessage = "Department must not exceed 100 characters.")]
    public string? Department { get; init; }

    [MaxLength(30, ErrorMessage = "Phone must not exceed 30 characters.")]
    public string? Phone { get; init; }

    [MaxLength(150, ErrorMessage = "Email must not exceed 150 characters.")]
    [EmailAddress(ErrorMessage = "Email is not valid.")]
    public string? Email { get; init; }

    [MaxLength(250, ErrorMessage = "Website must not exceed 250 characters.")]
    public string? Website { get; init; }

    [MaxLength(50, ErrorMessage = "FacilityRegistrationCode must not exceed 50 characters.")]
    public string? FacilityRegistrationCode { get; init; }
}
