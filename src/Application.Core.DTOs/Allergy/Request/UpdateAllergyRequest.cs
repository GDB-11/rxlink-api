using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Allergy.Request;

public sealed record UpdateAllergyRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(150, ErrorMessage = "Name must not exceed 150 characters.")]
    public required string Name { get; init; }

    public string? Description { get; init; }
}