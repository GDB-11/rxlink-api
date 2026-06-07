namespace Application.Core.DTOs.PatientAuth.Response;

public sealed record PatientInfo
{
    public required Guid PatientCode { get; init; }
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public required string Email { get; init; }
    public required string MedicalRecordNumber { get; init; }
}
