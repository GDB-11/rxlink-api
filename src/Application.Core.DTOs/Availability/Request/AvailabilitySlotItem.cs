using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Availability.Request;

public sealed record AvailabilitySlotItem
{
    [Required(ErrorMessage = "Date is required.")]
    public required DateOnly Date { get; init; }

    [Required(ErrorMessage = "StartTime is required.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "StartTime must be in HH:MM format.")]
    public required string StartTime { get; init; }
}