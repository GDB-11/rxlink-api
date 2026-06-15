using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.User;
using Infrastructure.Core.Interfaces.Users;
using Infrastructure.Core.Models.Users;

namespace Infrastructure.Core.Services.Users;

/// <summary>
/// Handles all database operations for platform user management.
/// </summary>
public sealed class UserRepository : BaseDatabaseService, IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<UserRow>, UserRepositoryError>> GetPageAsync(
        int offset, int limit, string? search, string? role = null) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, UserRow>(
                _connection,
                UserRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search, Role = role }),
            errorFactory: UserRepositoryError (ex) => new GetUsersPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<UserRow?, UserRepositoryError>> GetByCodeAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, UserRow>(
                _connection,
                UserRepositorySql.GetByCode,
                new { Code = code }),
            errorFactory: UserRepositoryError (ex) => new GetUsersPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<UserRow?, UserRepositoryError>> InsertAsync(
        Guid personCode,
        string roleName, Guid? specialtyCode,
        string username, string email, string passwordHash, string? licenseNumber) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, UserRow>(
                _connection,
                UserRepositorySql.Insert,
                new
                {
                    PersonCode = personCode,
                    RoleName = roleName,
                    SpecialtyCode = specialtyCode,
                    Username = username,
                    Email = email,
                    PasswordHash = passwordHash,
                    LicenseNumber = licenseNumber
                }),
            errorFactory: UserRepositoryError (ex) => new InsertUserError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<UserRow?, UserRepositoryError>> UpdateAsync(
        Guid code,
        string roleName, Guid? specialtyCode,
        string username, string email, string? licenseNumber) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, UserRow>(
                _connection,
                UserRepositorySql.Update,
                new
                {
                    Code = code,
                    RoleName = roleName,
                    SpecialtyCode = specialtyCode,
                    Username = username,
                    Email = email,
                    LicenseNumber = licenseNumber
                }),
            errorFactory: UserRepositoryError (ex) => new UpdateUserError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<UserRow?, UserRepositoryError>> UpdateRoleAsync(Guid code, string roleName) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, UserRow>(
                _connection,
                UserRepositorySql.UpdateRole,
                new { Code = code, RoleName = roleName }),
            errorFactory: UserRepositoryError (ex) => new UpdateUserError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, UserRepositoryError>> DeactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                UserRepositorySql.Deactivate,
                new { Code = code, UserCode = performedByUserCode }),
            errorFactory: UserRepositoryError (ex) => new DeactivateUserError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, UserRepositoryError>> ActivateAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                UserRepositorySql.Activate,
                new { Code = code }),
            errorFactory: UserRepositoryError (ex) => new ActivateUserError(ex.Message, ex)
        );
}