namespace Application.Core.DTOs.Appointment.Request;

public sealed record AdminAppointmentPageRequest(
    int Page = 1,
    int PageSize = 10,
    string? PatientSearch = null,
    DateOnly? Date = null,
    string? StatusName = null);
