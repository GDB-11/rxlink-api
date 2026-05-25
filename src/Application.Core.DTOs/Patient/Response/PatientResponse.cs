namespace Application.Core.DTOs.Patient.Response;

public sealed class PatientResponse
{
    public required Guid PatientCode { get; init; }
    public required string MedicalRecordNumber { get; init; }
    public required bool IsActive { get; init; }
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
