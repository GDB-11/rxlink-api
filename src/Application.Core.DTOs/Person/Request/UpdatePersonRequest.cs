using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Person.Request;

public sealed record UpdatePersonRequest
{
    [Required(ErrorMessage = "Names is required.")]
    [MaxLength(200, ErrorMessage = "Names must not exceed 200 characters.")]
    public required string Names { get; init; }

    [Required(ErrorMessage = "Surnames is required.")]
    [MaxLength(150, ErrorMessage = "Surnames must not exceed 150 characters.")]
    public required string Surnames { get; init; }

    [Required(ErrorMessage = "BirthDate is required.")]
    public required DateOnly BirthDate { get; init; }

    [Required(ErrorMessage = "SexCode is required.")]
    public required Guid SexCode { get; init; }

    [Required(ErrorMessage = "Phone is required.")]
    [MaxLength(30, ErrorMessage = "Phone must not exceed 30 characters.")]
    public required string Phone { get; init; }

    [MaxLength(30, ErrorMessage = "AlternativePhone must not exceed 30 characters.")]
    public string? AlternativePhone { get; init; }

    [Required(ErrorMessage = "Email is required.")]
    [MaxLength(254, ErrorMessage = "Email must not exceed 254 characters.")]
    [EmailAddress(ErrorMessage = "Email is not valid.")]
    public required string Email { get; init; }

    public string? Address { get; init; }

    [MaxLength(200, ErrorMessage = "EmergencyContactName must not exceed 200 characters.")]
    public string? EmergencyContactName { get; init; }

    [MaxLength(30, ErrorMessage = "EmergencyContactPhone must not exceed 30 characters.")]
    public string? EmergencyContactPhone { get; init; }

    [Required(ErrorMessage = "DocumentTypeCode is required.")]
    public required Guid DocumentTypeCode { get; init; }

    [Required(ErrorMessage = "DocumentNumber is required.")]
    [MaxLength(50, ErrorMessage = "DocumentNumber must not exceed 50 characters.")]
    public required string DocumentNumber { get; init; }
}
