namespace Infrastructure.Core.Models.PrivacyNotice;

public sealed class PrivacyNoticeRow
{
    public required Guid PrivacyNoticeCode { get; init; }
    public required int Version { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required DateOnly EffectiveFrom { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
    public long TotalCount { get; init; }
}

public sealed class PrivacyNoticeAcknowledgmentRow
{
    public required Guid PrivacyNoticeCode { get; init; }
    public required int Version { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset AcknowledgedAt { get; init; }
    public string? RecordedByName { get; init; }
}
