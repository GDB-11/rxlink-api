namespace Infrastructure.Core.Models.PatientAuth;

public sealed class PatientCredential
{
    public required Guid PatientCode { get; init; }
    public required Guid PersonCode { get; init; }
    public required string Email { get; init; }
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public required string MedicalRecordNumber { get; init; }
    public string? PasswordHash { get; init; }
    public required bool IsActive { get; init; }
}
