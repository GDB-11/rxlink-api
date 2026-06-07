using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.PatientAuth.Request;

public sealed record RegisterPatientRequest
{
    public Guid? PersonCode { get; init; }

    [Required]
    [MaxLength(200)]
    public required string Names { get; init; }

    [Required]
    [MaxLength(150)]
    public required string Surnames { get; init; }

    [Required]
    public required DateOnly BirthDate { get; init; }

    [Required]
    public required Guid SexCode { get; init; }

    [Required]
    [MaxLength(30)]
    public required string Phone { get; init; }

    [MaxLength(30)]
    public string? AlternativePhone { get; init; }

    [Required]
    [MaxLength(254)]
    [EmailAddress]
    public required string Email { get; init; }

    public string? Address { get; init; }

    [MaxLength(200)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(30)]
    public string? EmergencyContactPhone { get; init; }

    [Required]
    public required Guid DocumentTypeCode { get; init; }

    [Required]
    [MaxLength(50)]
    public required string DocumentNumber { get; init; }

    [Required]
    [MinLength(8)]
    public required string Password { get; init; }
}
