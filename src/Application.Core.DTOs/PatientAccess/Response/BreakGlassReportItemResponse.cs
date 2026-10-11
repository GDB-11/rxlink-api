namespace Application.Core.DTOs.PatientAccess.Response;

public sealed class BreakGlassReportItemResponse
{
    public required Guid GrantCode { get; init; }
    public required Guid UserCode { get; init; }
    public required string Username { get; init; }
    public required Guid PatientCode { get; init; }
    public required string MedicalRecordNumber { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset GrantedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
