using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Specialty.Request;

public sealed record CreateSpecialtyRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name must not exceed 100 characters.")]
    public required string Name { get; init; }

    [Range(0.01, 999999.99, ErrorMessage = "PriceInPerson must be greater than 0.")]
    public required decimal PriceInPerson { get; init; }

    [Range(0.01, 999999.99, ErrorMessage = "PriceVirtual must be greater than 0.")]
    public required decimal PriceVirtual { get; init; }
}