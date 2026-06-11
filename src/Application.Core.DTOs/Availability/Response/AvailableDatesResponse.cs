namespace Application.Core.DTOs.Availability.Response;

public sealed record AvailableDatesResponse
{
    public required Guid DoctorCode { get; init; }
    public required IReadOnlyList<DateOnly> AvailableDates { get; init; }
}