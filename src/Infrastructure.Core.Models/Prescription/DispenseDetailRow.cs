namespace Infrastructure.Core.Models.Prescription;

/// <summary>A prescription detail as needed to write its <c>Dispense</c> stock movement.</summary>
public sealed class DispenseDetailRow
{
    public required int PrescriptionDetailId { get; init; }
    public required Guid PrescriptionDetailCode { get; init; }
    public required Guid MedicationCode { get; init; }
}
