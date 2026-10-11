namespace Infrastructure.Core.Models.Report;

public sealed class ActivityByUserRow
{
    public Guid? UserCode { get; init; }
    public string? Username { get; init; }
    public required string EventType { get; init; }
    public required long EventCount { get; init; }
    public required DateTimeOffset LastOccurredAt { get; init; }
}
