namespace Application.Core.DTOs.Appointment.Response;

public sealed record AppointmentResponse(
    Guid AppointmentCode,
    Guid PatientCode,
    string PatientNames,
    string PatientSurnames,
    Guid DoctorCode,
    string DoctorNames,
    string DoctorSurnames,
    string SpecialtyName,
    string ConsultationTypeName,
    string StatusName,
    Guid StatusCode,
    DateTimeOffset ScheduledAt,
    string Date,
    string Time,
    DateTimeOffset CreatedAt);