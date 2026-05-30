using BindSharp;
using Infrastructure.Core.DTOs.Person;
using Infrastructure.Core.Models.Person;

namespace Infrastructure.Core.Interfaces.Person;

public interface IPersonRepository
{
    /// <summary>Returns one page of persons, with a total count via window function.</summary>
    Task<Result<IEnumerable<PersonRow>, PersonRepositoryError>> GetPageAsync(int offset, int limit, string? search);

    /// <summary>Inserts a new person and returns the created row, or <c>null</c> on unexpected failure.</summary>
    Task<Result<PersonRow?, PersonRepositoryError>> InsertAsync(
        string names, string surnames, DateOnly birthDate, Guid sexCode,
        string phone, string? alternativePhone, string email,
        string? address, string? emergencyContactName, string? emergencyContactPhone);

    /// <summary>Updates a person by code. Returns <c>null</c> when no matching row exists.</summary>
    Task<Result<PersonRow?, PersonRepositoryError>> UpdateAsync(
        Guid code, string names, string surnames, DateOnly birthDate, Guid sexCode,
        string phone, string? alternativePhone, string email,
        string? address, string? emergencyContactName, string? emergencyContactPhone);
}
