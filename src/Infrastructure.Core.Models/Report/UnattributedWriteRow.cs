namespace Infrastructure.Core.Models.Report;

public sealed class UnattributedWriteRow
{
    public required string TableName { get; init; }
    public required string Action { get; init; }
    public required long WriteCount { get; init; }
    public required DateTimeOffset LastOccurredAt { get; init; }
}
