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
    Task<Result<Unit, CredentialError>> UpdateRefreshTokenAsync(Guid userCode, string tokenHash, DateTime expiresAt);
 
    /// <summary>
    /// Revokes all active refresh tokens for the user identified by <paramref name="userCode"/>
    /// (used on explicit logout).
    /// </summary>
    Task<Result<Unit, CredentialError>> ClearRefreshTokenAsync(Guid userCode);
}