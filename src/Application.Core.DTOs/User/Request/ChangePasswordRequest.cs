using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.User.Request;

public sealed record ChangePasswordRequest
{
    [Required(ErrorMessage = "CurrentPassword is required.")]
    public required string CurrentPassword { get; init; }

    [Required(ErrorMessage = "NewPassword is required.")]
    [MinLength(8, ErrorMessage = "NewPassword must be at least 8 characters long.")]
    public required string NewPassword { get; init; }
}