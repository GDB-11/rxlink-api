using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Availability.Request;

public sealed record GetAvailabilityRequest
{
    [Required(ErrorMessage = "Month is required.")]
    [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Month must be in YYYY-MM format.")]
    public required string Month { get; init; }
}