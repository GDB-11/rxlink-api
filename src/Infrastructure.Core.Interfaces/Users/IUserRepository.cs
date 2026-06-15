using BindSharp;
using Infrastructure.Core.DTOs.User;
using Infrastructure.Core.Models.Users;

namespace Infrastructure.Core.Interfaces.Users;

public interface IUserRepository
{
    /// <summary>Returns one page of users, with a total count via window function. Optionally filtered by role name.</summary>
    Task<Result<IEnumerable<UserRow>, UserRepositoryError>> GetPageAsync(int offset, int limit, string? search,
        string? role = null);

    /// <summary>Returns a single user by their public code, or null when not found or soft-deleted.</summary>
    Task<Result<UserRow?, UserRepositoryError>> GetByCodeAsync(Guid code);

    /// <summary>
    /// Inserts a User linked to an existing Person identified by <paramref name="personCode"/>.
    /// All catalogue lookups use public UUIDs (codes); internal IDs are resolved in SQL.
    /// Returns <c>null</c> when PersonCode is not found or any catalogue code (role) is invalid.
    /// Throws on unique constraint violations (duplicate username/email).
    /// </summary>
    Task<Result<UserRow?, UserRepositoryError>> InsertAsync(
        Guid personCode,
        string roleName, Guid? specialtyCode,
        string username, string email, string passwordHash, string? licenseNumber);

    /// <summary>
    /// Updates account fields of an existing User. Person data is immutable through this endpoint.
    /// Returns <c>null</c> when the user is not found, deleted, or any catalog code is invalid.
    /// </summary>
    Task<Result<UserRow?, UserRepositoryError>> UpdateAsync(
        Guid code,
        string roleName, Guid? specialtyCode,
        string username, string email, string? licenseNumber);

    /// <summary>
    /// Updates only the RoleId of an existing active user.
    /// Specialty, username, email and license are preserved.
    /// Returns <c>null</c> when the user is not found/deleted or the role name is invalid/inactive.
    /// </summary>
    Task<Result<UserRow?, UserRepositoryError>> UpdateRoleAsync(Guid code, string roleName);

    /// <summary>Soft-deletes an active user. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, UserRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Reactivates a previously deactivated user. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, UserRepositoryError>> ActivateAsync(Guid code);
}