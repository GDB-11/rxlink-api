namespace Application.Core.Config;

/// <summary>Account lockout and session limits for staff accounts (Phase 2b.3).</summary>
public sealed record SessionSecurityConfig
{
    /// <summary>Consecutive failed credential attempts (password or MFA code) before the account is locked.</summary>
    public int MaxFailedAttempts { get; init; } = 5;

    /// <summary>How long a lockout lasts.</summary>
    public int LockoutMinutes { get; init; } = 15;

    /// <summary>Hard cap on a login session, however often the refresh token is rotated. 0 disables the cap.</summary>
    public int AbsoluteSessionMinutes { get; init; } = 720;
}
