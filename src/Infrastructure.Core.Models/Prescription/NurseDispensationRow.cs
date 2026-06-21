namespace Infrastructure.Core.Models.Prescription;

public sealed class NurseDispensationRow
{
    public required Guid PrescriptionCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required string PatientNames { get; init; }
    public required string PatientSurnames { get; init; }
    public required string DiagnosticDescription { get; init; }
    public required DateTimeOffset DispensedAt { get; init; }
    public required int DetailCount { get; init; }
    public required string MedicationNames { get; init; }
}