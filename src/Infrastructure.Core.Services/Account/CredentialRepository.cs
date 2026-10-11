using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Account;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Account;

namespace Infrastructure.Core.Services.Account;

/// <summary>
/// Handles all database operations related to user authentication and
/// refresh-token lifecycle (issue, rotate, revoke).
/// </summary>
public sealed class CredentialRepository : BaseDatabaseService, ICredentialRepository
{
    private readonly IDbConnection _connection;

    public CredentialRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<User?, CredentialError>> GetByUsernameAsync(string username) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, User?>(
                _connection,
                CredentialRepositorySql.GetByUsername,
                new { Username = username }),
            errorFactory: CredentialError (ex) => new GetByUsernameAsyncError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<User?, CredentialError>> GetByCodeAsync(Guid userCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, User?>(
                _connection,
                CredentialRepositorySql.GetByCode,
                new { UserCode = userCode }),
            errorFactory: CredentialError (ex) => new GetByCodeAsyncError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<User?, CredentialError>> GetByRefreshTokenAsync(string tokenHash) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, User?>(
                _connection,
                CredentialRepositorySql.GetByRefreshToken,
                new
                {
                    TokenHash = tokenHash,
                    CurrentDate = DateTime.UtcNow
                }),
            errorFactory: CredentialError (ex) => new GetByRefreshTokenAsyncError(ex.Message, ex)
        );

    /// <inheritdoc/>
    /// <remarks>
    /// Executes token rotation atomically via a CTE:
    /// all currently-active tokens for the user are revoked, then the new
    /// hashed token is inserted in the same round-trip.
    /// </remarks>
    public async Task<Result<Unit, CredentialError>> UpdateRefreshTokenAsync(
        Guid userCode,
        string tokenHash,
        DateTime expiresAt,
        DateTime sessionStartedAt) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                CredentialRepositorySql.UpdateRefreshToken,
                new
                {
                    UserCode = userCode,
                    TokenHash = tokenHash,
                    ExpiresAt = expiresAt,
                    SessionStartedAt = sessionStartedAt,
                    RevokedAt = DateTime.UtcNow
                }),
            errorFactory: CredentialError (ex) => new UpdateRefreshTokenAsyncError(ex.Message, ex)
        ).BindAsync(affectedRows => ValidateAffectedRows<CredentialError>(
            affectedRows,
            msg => new UpdateRefreshTokenAsyncError(msg),
            "The refresh token could not be issued — no rows were inserted."
        ));

    /// <inheritdoc/>
    /// <remarks>
    /// Marks every active refresh token as revoked. Returns a success even
    /// when no tokens were active (idempotent logout).
    /// </remarks>
    public async Task<Result<Unit, CredentialError>> ClearRefreshTokenAsync(Guid userCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                CredentialRepositorySql.ClearRefreshToken,
                new
                {
                    UserCode = userCode,
                    RevokedAt = DateTime.UtcNow
                }),
            errorFactory: CredentialError (ex) => new ClearRefreshTokenAsyncError(ex.Message, ex)
        ).MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public async Task<Result<Guid?, CredentialError>> GetRevokedRefreshTokenOwnerAsync(string tokenHash) =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, Guid?>(
                _connection,
                CredentialRepositorySql.GetRevokedRefreshTokenOwner,
                new { TokenHash = tokenHash }),
            errorFactory: CredentialError (ex) => new GetByRefreshTokenAsyncError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<DateTime?, CredentialError>> RecordFailedLoginAsync(
        Guid userCode, int maxAttempts, DateTime now, DateTime lockedUntil) =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, DateTime?>(
                _connection,
                CredentialRepositorySql.RecordFailedLogin,
                new { UserCode = userCode, MaxAttempts = maxAttempts, Now = now, LockedUntil = lockedUntil }),
            errorFactory: CredentialError (ex) => new RecordFailedLoginAsyncError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<Unit, CredentialError>> ResetFailedLoginsAsync(Guid userCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                CredentialRepositorySql.ResetFailedLogins,
                new { UserCode = userCode }),
            errorFactory: CredentialError (ex) => new ResetFailedLoginsAsyncError(ex.Message, ex)
        ).MapAsync(_ => Unit.Value);
}
