namespace Application.Core.DTOs.Appointment.Request;

public sealed record AppointmentPageRequest(
    int Page = 1,
    int PageSize = 10);