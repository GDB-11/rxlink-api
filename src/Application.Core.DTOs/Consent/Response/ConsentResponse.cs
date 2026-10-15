namespace Application.Core.DTOs.Consent.Response;

public sealed class ConsentResponse
{
    public required Guid ConsentCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required string ConsentType { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset GrantedAt { get; init; }
    public required string RecordedByName { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }

    /// <summary><c>null</c> when revoked by the patient themselves (or not revoked).</summary>
    public string? RevokedByName { get; init; }
    public string? RevokedReason { get; init; }
}

public sealed class ConsentPageResponse
{
    public required List<ConsentResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
