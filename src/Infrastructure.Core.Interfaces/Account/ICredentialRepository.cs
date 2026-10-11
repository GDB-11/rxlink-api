using BindSharp;
using Infrastructure.Core.DTOs.Account;
using Infrastructure.Core.Models.Account;

namespace Infrastructure.Core.Interfaces.Account;

public interface ICredentialRepository
{
    /// <summary>Returns the active user matching <paramref name="username"/>, or <c>null</c> if not found.</summary>
    Task<Result<User?, CredentialError>> GetByUsernameAsync(string username);

    /// <summary>Returns the active user matching <paramref name="userCode"/>, or <c>null</c> if not found.</summary>
    Task<Result<User?, CredentialError>> GetByCodeAsync(Guid userCode);

    /// <summary>
    /// Looks up the user whose active (non-revoked, non-expired) refresh-token hash matches
    /// <paramref name="tokenHash"/>. Returns <c>null</c> if the token is invalid or expired.
    /// </summary>
    Task<Result<User?, CredentialError>> GetByRefreshTokenAsync(string tokenHash);

    /// <summary>
    /// Rotates the refresh token for the user identified by <paramref name="userCode"/>:
    /// revokes every currently-active token and inserts a new one.
    /// <paramref name="tokenHash"/> must already be hashed by the caller.
    /// </summary>
    /// <paramref name="sessionStartedAt"/> is the login time of the session: a new login passes "now", a refresh
    /// carries over the value of the token it replaces.
    Task<Result<Unit, CredentialError>> UpdateRefreshTokenAsync(
        Guid userCode, string tokenHash, DateTime expiresAt, DateTime sessionStartedAt);

    /// <summary>
    /// Revokes all active refresh tokens for the user identified by <paramref name="userCode"/>
    /// (used on explicit logout).
    /// </summary>
    Task<Result<Unit, CredentialError>> ClearRefreshTokenAsync(Guid userCode);

    /// <summary>
    /// Returns the <c>UserCode</c> that owns a refresh token that has already been revoked (rotated or
    /// logged out), or <c>null</c> if no revoked token matches <paramref name="tokenHash"/>. Presenting such
    /// a token again is treated as token reuse.
    /// </summary>
    Task<Result<Guid?, CredentialError>> GetRevokedRefreshTokenOwnerAsync(string tokenHash);

    /// <summary>
    /// Registers a failed credential attempt (wrong password or wrong MFA code). Returns the lock expiry when this
    /// attempt reached <paramref name="maxAttempts"/> (or the account was already locked), otherwise <c>null</c>.
    /// </summary>
    Task<Result<DateTime?, CredentialError>> RecordFailedLoginAsync(
        Guid userCode, int maxAttempts, DateTime now, DateTime lockedUntil);

    /// <summary>Clears the failed-attempt counter after a completed login.</summary>
    Task<Result<Unit, CredentialError>> ResetFailedLoginsAsync(Guid userCode);
}
