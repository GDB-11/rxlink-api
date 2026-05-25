using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.User.Request;

public sealed record UpdateUserRequest
{
    // Person
    [Required(ErrorMessage = "Names is required.")]
    [MaxLength(200, ErrorMessage = "Names must not exceed 200 characters.")]
    public required string Names { get; init; }

    [Required(ErrorMessage = "Surnames is required.")]
    [MaxLength(150, ErrorMessage = "Surnames must not exceed 150 characters.")]
    public required string Surnames { get; init; }

    public required DateOnly BirthDate { get; init; }

    public required Guid SexCode { get; init; }

    [Required(ErrorMessage = "Phone is required.")]
    [MaxLength(30, ErrorMessage = "Phone must not exceed 30 characters.")]
    public required string Phone { get; init; }

    [MaxLength(30, ErrorMessage = "AlternativePhone must not exceed 30 characters.")]
    public string? AlternativePhone { get; init; }

    [Required(ErrorMessage = "PersonEmail is required.")]
    [MaxLength(254, ErrorMessage = "PersonEmail must not exceed 254 characters.")]
    [EmailAddress(ErrorMessage = "PersonEmail must be a valid email address.")]
    public required string PersonEmail { get; init; }

    public string? Address { get; init; }

    [MaxLength(200, ErrorMessage = "EmergencyContactName must not exceed 200 characters.")]
    public string? EmergencyContactName { get; init; }

    [MaxLength(30, ErrorMessage = "EmergencyContactPhone must not exceed 30 characters.")]
    public string? EmergencyContactPhone { get; init; }

    // PersonDocument
    public required Guid DocumentTypeCode { get; init; }

    [Required(ErrorMessage = "DocumentNumber is required.")]
    [MaxLength(50, ErrorMessage = "DocumentNumber must not exceed 50 characters.")]
    public required string DocumentNumber { get; init; }

    public DateOnly? DocumentIssueDate { get; init; }
    public DateOnly? DocumentExpirationDate { get; init; }

    // User account (password is not changed through this endpoint)
    [Required(ErrorMessage = "RoleName is required.")]
    public required string RoleName { get; init; }

    public Guid? SpecialtyCode { get; init; }

    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(100, ErrorMessage = "Username must not exceed 100 characters.")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "Email is required.")]
    [MaxLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    public required string Email { get; init; }

    [MaxLength(100, ErrorMessage = "LicenseNumber must not exceed 100 characters.")]
    public string? LicenseNumber { get; init; }
}
