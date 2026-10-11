namespace Infrastructure.Core.Models.PatientAccess;

/// <summary>Result of a break-glass request. <c>GrantCode</c>/<c>ExpiresAt</c> are null when no grant was needed.</summary>
public sealed class BreakGlassGrantRow
{
    public required Guid PatientCode { get; init; }
    public required bool InCareTeam { get; init; }
    public Guid? GrantCode { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
