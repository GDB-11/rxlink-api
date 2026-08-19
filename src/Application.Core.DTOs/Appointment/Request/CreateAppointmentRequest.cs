namespace Application.Core.DTOs.Appointment.Request;

public sealed record CreateAppointmentRequest(
    Guid AvailabilityCode,
    Guid ConsultationTypeCode,
    bool PayNow = false,
    Guid? InsuranceCode = null);