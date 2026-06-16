namespace Infrastructure.Core.Models.Availability;

public sealed class AvailabilityDeleteCheckRow
{
    public required bool IsBooked { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
}