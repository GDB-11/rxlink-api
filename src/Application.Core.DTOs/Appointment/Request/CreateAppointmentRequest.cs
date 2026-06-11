namespace Application.Core.DTOs.Appointment.Request;

public sealed record CreateAppointmentRequest(
    Guid AvailabilityCode,
    Guid ConsultationTypeCode);