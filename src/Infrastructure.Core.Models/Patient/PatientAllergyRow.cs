namespace Infrastructure.Core.Models.Patient;

/// <summary>
/// A patient's recorded allergy as stored in the partner database: codes only. Names come from the catalog snapshot.
/// </summary>
public sealed class PatientAllergyRow
{
    public required Guid PatientAllergyCode { get; init; }
    public required Guid AllergyCode { get; init; }
    public Guid? SeverityCode { get; init; }
    public string? Notes { get; init; }
}
