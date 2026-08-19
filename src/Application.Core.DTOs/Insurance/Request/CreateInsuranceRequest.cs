using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Insurance.Request;

public sealed record CreateInsuranceRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(100, ErrorMessage = "Name must not exceed 100 characters.")]
    public required string Name { get; init; }

    [Range(0, 100, ErrorMessage = "CoveragePercentage must be between 0 and 100.")]
    public required decimal CoveragePercentage { get; init; }
}
