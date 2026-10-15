namespace Infrastructure.Core.Models.Amendment;

public sealed class AmendmentRow
{
    public required Guid AmendmentRequestCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required string PatientName { get; init; }
    public required string MedicalRecordNumber { get; init; }
    public required string TargetArea { get; init; }
    public required string RequestedChange { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }
    public string? ReviewedByName { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public string? DecisionReason { get; init; }
    public string? AmendmentText { get; init; }
    public long TotalCount { get; init; }
}
