using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Availability.Request;

public sealed record CreateAvailabilityRequest
{
    [Required(ErrorMessage = "Slots is required.")]
    [MinLength(1, ErrorMessage = "At least one slot is required.")]
    public required IReadOnlyList<AvailabilitySlotItem> Slots { get; init; }
}