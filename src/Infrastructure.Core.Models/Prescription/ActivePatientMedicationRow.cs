namespace Infrastructure.Core.Models.Prescription;

/// <summary>A medication on one of the patient's currently <c>Activo</c> prescriptions.</summary>
public sealed class ActivePatientMedicationRow
{
    public required Guid PrescriptionCode { get; init; }
    public required Guid MedicationCode { get; init; }
}
