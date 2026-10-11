namespace Application.Core.DTOs.Report.Response;

/// <summary>Audited writes that landed with no actor, i.e. a write path that did not set the audit context.</summary>
public sealed class UnattributedWriteItemResponse
{
    public required string TableName { get; init; }
    public required string Action { get; init; }
    public required long WriteCount { get; init; }
    public required DateTimeOffset LastOccurredAt { get; init; }
}
