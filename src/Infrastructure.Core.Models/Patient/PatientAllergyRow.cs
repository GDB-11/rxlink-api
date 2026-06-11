namespace Infrastructure.Core.Models.Patient;

public sealed class PatientAllergyRow
{
    public required Guid PatientAllergyCode { get; init; }
    public required Guid AllergyCode { get; init; }
    public required string AllergyName { get; init; }
    public Guid? SeverityCode { get; init; }
    public string? SeverityName { get; init; }
    public string? Notes { get; init; }
}