namespace Infrastructure.Core.Models.Prescription;

public sealed class DoctorDraftPrescriptionRow
{
    public required Guid PrescriptionCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required string PatientNames { get; init; }
    public required string PatientSurnames { get; init; }
    public required string DiagnosticDescription { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int DetailCount { get; init; }
}