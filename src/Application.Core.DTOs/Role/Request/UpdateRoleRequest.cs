using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Role.Request;

public sealed record UpdateRoleRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(50, ErrorMessage = "Name must not exceed 50 characters.")]
    public required string Name { get; init; }

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    public string? Description { get; init; }
}