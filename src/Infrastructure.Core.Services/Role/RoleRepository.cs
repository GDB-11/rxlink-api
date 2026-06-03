using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Role;
using Infrastructure.Core.Interfaces.Role;
using Infrastructure.Core.Models.Role;

namespace Infrastructure.Core.Services.Role;

public sealed class RoleRepository : BaseDatabaseService, IRoleRepository
{
    private readonly IDbConnection _connection;

    public RoleRepository(IDbConnection connection)
    {
        _connection = connection;
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
            operation: async () => await ExecuteFirstOrDefaultAsync<object, RoleRow>(
                _connection,
                RoleRepositorySql.Insert,
                new { Name = name, Description = description }),
            errorFactory: RoleRepositoryError (ex) => new InsertRoleError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<RoleRow?, RoleRepositoryError>> UpdateAsync(
        Guid code, string name, string? description) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, RoleRow>(
                _connection,
                RoleRepositorySql.Update,
                new { Code = code, Name = name, Description = description }),
            errorFactory: RoleRepositoryError (ex) => new UpdateRoleError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, RoleRepositoryError>> DeactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                RoleRepositorySql.Deactivate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: RoleRepositoryError (ex) => new DeactivateRoleError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, RoleRepositoryError>> ActivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                RoleRepositorySql.Activate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: RoleRepositoryError (ex) => new ActivateRoleError(ex.Message, ex)
        );
}