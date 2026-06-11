namespace Application.Core.DTOs.Appointment.Response;

public sealed record AppointmentPageResponse(
    IReadOnlyList<AppointmentResponse> Items,
    int Total,
    int Page,
    int PageSize);