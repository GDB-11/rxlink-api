using BindSharp;
using Infrastructure.Core.DTOs.User;
using Infrastructure.Core.Models.Users;

namespace Infrastructure.Core.Interfaces.Users;

public interface IUserRepository
{
    /// <summary>Returns one page of users, with a total count via window function.</summary>
    Task<Result<IEnumerable<UserRow>, UserRepositoryError>> GetPageAsync(int offset, int limit, string? search);

    /// <summary>
    /// Inserts a Person, PersonDocument and User in a single CTE chain.
    /// All catalogue lookups use public UUIDs (codes); internal IDs are resolved in SQL.
    /// Returns <c>null</c> when any catalogue code (sex, document type, role) is invalid.
    /// Throws on unique constraint violations (duplicate username/email).
    /// </summary>
    Task<Result<UserRow?, UserRepositoryError>> InsertAsync(
        string names, string surnames, DateOnly birthDate, Guid sexCode,
        string phone, string? alternativePhone, string personEmail,
        string? address, string? emergencyContactName, string? emergencyContactPhone,
        Guid documentTypeCode, string documentNumber,
        DateOnly? documentIssueDate, DateOnly? documentExpirationDate,
        string roleName, Guid? specialtyCode,
        string username, string email, string passwordHash, string? licenseNumber);

    /// <summary>
    /// Updates Person, PersonDocument and User atomically.
    /// All catalogue lookups use public UUIDs (codes).
    /// Returns <c>null</c> when the user is not found, deleted, or any catalog code is invalid.
    /// </summary>
    Task<Result<UserRow?, UserRepositoryError>> UpdateAsync(
        Guid code,
        string names, string surnames, DateOnly birthDate, Guid sexCode,
        string phone, string? alternativePhone, string personEmail,
        string? address, string? emergencyContactName, string? emergencyContactPhone,
        Guid documentTypeCode, string documentNumber,
        DateOnly? documentIssueDate, DateOnly? documentExpirationDate,
        string roleName, Guid? specialtyCode,
        string username, string email, string? licenseNumber);

    /// <summary>Soft-deletes an active user. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, UserRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);
    
    /// <summary>Reactivates a previously deactivated user. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, UserRepositoryError>> ActivateAsync(Guid code);
}
