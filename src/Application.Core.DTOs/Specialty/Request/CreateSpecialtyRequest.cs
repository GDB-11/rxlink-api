using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Specialty.Request;

public sealed record CreateSpecialtyRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name must not exceed 100 characters.")]
    public required string Name { get; init; }
}