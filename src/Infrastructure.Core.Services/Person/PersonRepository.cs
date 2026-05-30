using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Person;
using Infrastructure.Core.Interfaces.Person;
using Infrastructure.Core.Models.Person;

namespace Infrastructure.Core.Services.Person;

public sealed class PersonRepository : BaseDatabaseService, IPersonRepository
{
    private readonly IDbConnection _connection;

    public PersonRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<PersonRow>, PersonRepositoryError>> GetPageAsync(
        int offset, int limit, string? search) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, PersonRow>(
                _connection,
                PersonRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search }),
            errorFactory: PersonRepositoryError (ex) => new GetPersonsPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PersonRow?, PersonRepositoryError>> InsertAsync(
        string names, string surnames, DateOnly birthDate, Guid sexCode,
        string phone, string? alternativePhone, string email,
        string? address, string? emergencyContactName, string? emergencyContactPhone) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PersonRow>(
                _connection,
                PersonRepositorySql.Insert,
                new
                {
                    Names = names,
                    Surnames = surnames,
                    BirthDate = birthDate,
                    SexCode = sexCode,
                    Phone = phone,
                    AlternativePhone = alternativePhone,
                    Email = email,
                    Address = address,
                    EmergencyContactName = emergencyContactName,
                    EmergencyContactPhone = emergencyContactPhone
                }),
            errorFactory: PersonRepositoryError (ex) => new InsertPersonError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PersonRow?, PersonRepositoryError>> UpdateAsync(
        Guid code, string names, string surnames, DateOnly birthDate, Guid sexCode,
        string phone, string? alternativePhone, string email,
        string? address, string? emergencyContactName, string? emergencyContactPhone) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PersonRow>(
                _connection,
                PersonRepositorySql.Update,
                new
                {
                    Code = code,
                    Names = names,
                    Surnames = surnames,
                    BirthDate = birthDate,
                    SexCode = sexCode,
                    Phone = phone,
                    AlternativePhone = alternativePhone,
                    Email = email,
                    Address = address,
                    EmergencyContactName = emergencyContactName,
                    EmergencyContactPhone = emergencyContactPhone
                }),
            errorFactory: PersonRepositoryError (ex) => new UpdatePersonError(ex.Message, ex)
        );
}
