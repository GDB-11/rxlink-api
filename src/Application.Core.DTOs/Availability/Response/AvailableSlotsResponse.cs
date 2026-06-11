namespace Application.Core.DTOs.Availability.Response;

public sealed record AvailableSlotsResponse
{
    public required Guid DoctorCode { get; init; }
    public required DateOnly Date { get; init; }
    public required IReadOnlyList<AvailableSlotItem> Slots { get; init; }
}