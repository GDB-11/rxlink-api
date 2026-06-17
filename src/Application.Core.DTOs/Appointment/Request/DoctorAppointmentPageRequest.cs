namespace Application.Core.DTOs.Appointment.Request;

public sealed record DoctorAppointmentPageRequest(
    int Page = 1,
    int PageSize = 10,
    DateOnly? Date = null,
    string? StatusName = null);