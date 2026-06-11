namespace Infrastructure.Core.Models.Availability;

public sealed class AvailabilityRow
{
    public required Guid DoctorAvailabilityCode { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required bool IsBooked { get; init; }
}