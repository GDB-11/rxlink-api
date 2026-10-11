using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Mfa;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Mfa;
using Infrastructure.Core.Models.Mfa;

namespace Infrastructure.Core.Services.Mfa;

public sealed class MfaRepository : BaseDatabaseService, IMfaRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public MfaRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<MfaRow?, MfaRepositoryError>> GetByUserCodeAsync(Guid userCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, MfaRow>(
                _connection, MfaRepositorySql.GetByUserCode, new { UserCode = userCode }),
            errorFactory: MfaRepositoryError (ex) => new GetMfaError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, MfaRepositoryError>> UpsertPendingAsync(Guid userCode, string secretEncrypted) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                MfaRepositorySql.UpsertPending, new { UserCode = userCode, Secret = secretEncrypted }),
            errorFactory: MfaRepositoryError (ex) => new UpsertPendingMfaError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, MfaRepositoryError>> ConfirmAsync(Guid userCode, long step) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                MfaRepositorySql.Confirm, new { UserCode = userCode, Step = step }),
            errorFactory: MfaRepositoryError (ex) => new ConfirmMfaError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, MfaRepositoryError>> AdvanceStepAsync(Guid userCode, long step) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                MfaRepositorySql.AdvanceStep, new { UserCode = userCode, Step = step }),
            errorFactory: MfaRepositoryError (ex) => new AdvanceMfaStepError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, MfaRepositoryError>> DeleteAsync(Guid userCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                MfaRepositorySql.Delete, new { UserCode = userCode }),
            errorFactory: MfaRepositoryError (ex) => new DeleteMfaError(ex.Message, ex)
        );
}
