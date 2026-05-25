using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Auth.Request;

public sealed record LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "Password is required.")]
    public required string Password { get; init; }
}