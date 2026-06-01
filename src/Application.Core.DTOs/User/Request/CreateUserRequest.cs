using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.User.Request;

public sealed record CreateUserRequest
{
    [Required(ErrorMessage = "PersonCode is required.")]
    public required Guid PersonCode { get; init; }

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

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 12 characters long.")]
    public required string Password { get; init; }

    [MaxLength(100, ErrorMessage = "LicenseNumber must not exceed 100 characters.")]
    public string? LicenseNumber { get; init; }
}
