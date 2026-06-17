namespace Application.Core.DTOs.Appointment.Request;

public sealed record AdminCreateAppointmentRequest(
    Guid PatientCode,
    Guid AvailabilityCode,
    Guid ConsultationTypeCode,
    bool IsPaid = false);