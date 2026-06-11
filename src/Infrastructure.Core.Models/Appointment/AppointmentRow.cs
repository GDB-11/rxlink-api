namespace Infrastructure.Core.Models.Appointment;

public sealed class AppointmentRow
{
    public required Guid AppointmentCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required Guid DoctorCode { get; init; }
    public required string DoctorNames { get; init; }
    public required string DoctorSurnames { get; init; }
    public required string SpecialtyName { get; init; }
    public required string ConsultationTypeName { get; init; }
    public required string StatusName { get; init; }
    public required Guid StatusCode { get; init; }
    public required DateTimeOffset ScheduledAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required long TotalCount { get; init; }
}