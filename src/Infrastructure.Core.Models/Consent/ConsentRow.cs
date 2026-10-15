namespace Infrastructure.Core.Models.Consent;

public sealed class ConsentRow
{
    public required Guid ConsentCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required string ConsentType { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset GrantedAt { get; init; }
    public required string RecordedByName { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
    public string? RevokedByName { get; init; }
    public string? RevokedReason { get; init; }
    public long TotalCount { get; init; }
}
