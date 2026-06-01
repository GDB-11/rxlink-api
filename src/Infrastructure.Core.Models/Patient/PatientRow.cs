namespace Infrastructure.Core.Models.Patient;

/// <summary>
/// Flat row returned by patient queries.
/// Combines columns from Patient and Person. AllergiesJson is a JSON array of allergy objects.
/// <c>TotalCount</c> is populated by the paginated query; single-row operations return 0.
/// </summary>
public sealed class PatientRow
{
    public required Guid PatientCode { get; init; }
    public required Guid PersonCode { get; init; }
    public required string MedicalRecordNumber { get; init; }
    public required bool IsActive { get; init; }
    public required long TotalCount { get; init; }
    public required string AllergiesJson { get; init; }

    // Person
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required string Phone { get; init; }
    public string? AlternativePhone { get; init; }
    public required string Email { get; init; }
    public string? Address { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
}
