namespace Application.Core.DTOs.Availability.Response;

public sealed record AvailableSlotItem
{
    public required Guid AvailabilityCode { get; init; }
    public required string Time { get; init; }
}