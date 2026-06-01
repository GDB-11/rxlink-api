using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.User.Request;

public sealed record UpdateUserRoleRequest
{
    [Required(ErrorMessage = "RoleName is required.")]
    [MaxLength(50, ErrorMessage = "RoleName must not exceed 50 characters.")]
    public required string RoleName { get; init; }
}