namespace Application.Core.DTOs.Availability.Response;

public sealed record AvailabilityResponse
{
    public required Guid AvailabilityCode { get; init; }
    public required DateOnly Date { get; init; }
    public required string StartTime { get; init; }
    public required bool IsBooked { get; init; }
    public required bool CanDelete { get; init; }
}