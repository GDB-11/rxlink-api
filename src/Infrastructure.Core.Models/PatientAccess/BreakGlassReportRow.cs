namespace Infrastructure.Core.Models.PatientAccess;

/// <summary>One break-glass grant, as shown to the administrator.</summary>
public sealed class BreakGlassReportRow
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
