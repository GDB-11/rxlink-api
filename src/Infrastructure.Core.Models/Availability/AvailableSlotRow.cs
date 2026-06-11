namespace Infrastructure.Core.Models.Availability;

public sealed class AvailableSlotRow
{
    public required Guid DoctorAvailabilityCode { get; init; }
    public required TimeOnly StartTime { get; init; }
}