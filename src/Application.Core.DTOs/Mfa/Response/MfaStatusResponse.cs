namespace Application.Core.DTOs.Mfa.Response;

public sealed class MfaStatusResponse
{
    /// <summary>True when a second factor is confirmed and login is challenged.</summary>
    public required bool Enabled { get; init; }

    /// <summary>True when an enrollment was started but not yet confirmed.</summary>
    public required bool Pending { get; init; }
}
