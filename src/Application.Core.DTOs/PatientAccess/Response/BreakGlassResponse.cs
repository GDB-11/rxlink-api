namespace Application.Core.DTOs.PatientAccess.Response;

public sealed class BreakGlassResponse
{
    public required Guid PatientCode { get; init; }

    /// <summary>Null when the patient was already in the caller's care team and no grant was needed.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
