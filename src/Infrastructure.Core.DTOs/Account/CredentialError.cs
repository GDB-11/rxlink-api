namespace Infrastructure.Core.DTOs.Account;

/// <summary>Base type for all credential-repository errors.</summary>
public abstract record CredentialError(string Message, string? Details = null, Exception? Exception = null);

// ── Query errors ─────────────────────────────────────────────────────────────

public sealed record GetByUsernameAsyncError(string? Details = null, Exception? Exception = null)
    : CredentialError("An unexpected error occurred while retrieving the user by username.", Details, Exception);

public sealed record GetByCodeAsyncError(string? Details = null, Exception? Exception = null)
    : CredentialError("An unexpected error occurred while retrieving the user by code.", Details, Exception);

public sealed record GetByRefreshTokenAsyncError(string? Details = null, Exception? Exception = null)
    : CredentialError("An unexpected error occurred while retrieving the user by refresh token.", Details, Exception);

// ── Mutation errors ───────────────────────────────────────────────────────────

public sealed record UpdateRefreshTokenAsyncError(string? Details = null, Exception? Exception = null)
    : CredentialError("An unexpected error occurred while updating the refresh token.", Details, Exception);

public sealed record ClearRefreshTokenAsyncError(string? Details = null, Exception? Exception = null)
    : CredentialError("An unexpected error occurred while revoking the refresh token.", Details, Exception);