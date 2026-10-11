using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Role;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Role;
using Infrastructure.Core.Models.Role;

namespace Infrastructure.Core.Services.Role;

public sealed class RoleRepository : BaseDatabaseService, IRoleRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public RoleRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<RoleRow>, RoleRepositoryError>> GetPageAsync(
        int offset, int limit, string? search) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, RoleRow>(
                _connection,
                RoleRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search }),
            errorFactory: RoleRepositoryError (ex) => new GetRolePageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<RoleRow?, RoleRepositoryError>> InsertAsync(
        string name, string? description) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, RoleRow>(_connection, _userContext,
                RoleRepositorySql.Insert,
                new { Name = name, Description = description }),
            errorFactory: RoleRepositoryError (ex) => new InsertRoleError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<RoleRow?, RoleRepositoryError>> UpdateAsync(
        Guid code, string name, string? description) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, RoleRow>(_connection, _userContext,
                RoleRepositorySql.Update,
                new { Code = code, Name = name, Description = description }),
            errorFactory: RoleRepositoryError (ex) => new UpdateRoleError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, RoleRepositoryError>> DeactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                RoleRepositorySql.Deactivate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: RoleRepositoryError (ex) => new DeactivateRoleError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, RoleRepositoryError>> ActivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                RoleRepositorySql.Activate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: RoleRepositoryError (ex) => new ActivateRoleError(ex.Message, ex)
        );
}