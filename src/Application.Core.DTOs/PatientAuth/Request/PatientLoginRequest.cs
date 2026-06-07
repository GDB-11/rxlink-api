using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.PatientAuth.Request;

public sealed record PatientLoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
