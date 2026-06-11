using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Availability.Request;

public sealed record AvailableSlotsRequest
{
    [Required(ErrorMessage = "Date is required.")]
    public required DateOnly Date { get; init; }
}