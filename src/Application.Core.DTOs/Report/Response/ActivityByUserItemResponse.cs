namespace Application.Core.DTOs.Report.Response;

public sealed class ActivityByUserItemResponse
{
    /// <summary>Null for events with no <c>User</c> actor (anonymous logins, patient self-reads, system).</summary>
    public Guid? UserCode { get; init; }
    public string? Username { get; init; }
    public required string EventType { get; init; }
    public required long EventCount { get; init; }
    public required DateTimeOffset LastOccurredAt { get; init; }
}
