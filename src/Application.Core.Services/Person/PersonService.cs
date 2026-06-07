using Application.Core.DTOs.Person.Errors;
using Application.Core.DTOs.Person.Request;
using Application.Core.DTOs.Person.Response;
using Application.Core.Interfaces.Person;
using BindSharp;
using BindSharp.Extensions;
using Common.Helpers;
using Infrastructure.Core.Interfaces.Person;
using Infrastructure.Core.Models.Person;

namespace Application.Core.Services.Person;

public sealed class PersonService : IPerson
{
    private readonly IPersonRepository _repository;

    public PersonService(IPersonRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<PersonPageResponse, PersonError>> GetPageAsync(PersonPageRequest request) =>
        _repository.GetPageAsync((request.Page - 1) * request.PageSize, request.PageSize, request.Search)
            .MapErrorAsync(PersonError (error) => new PersonDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));

    /// <inheritdoc/>
    public Task<Result<PersonPageResponse, PersonError>> GetAvailableAsync(PersonAvailableRequest request) =>
        _repository.GetAvailableAsync(
                (request.Page - 1) * request.PageSize, request.PageSize, request.Search,
                request.ExcludeLinkedUsers, request.ExcludeLinkedPatients)
            .MapErrorAsync(PersonError (error) => new PersonDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));

    /// <inheritdoc/>
    public Task<Result<PersonResponse, PersonError>> CreateAsync(CreatePersonRequest request) =>
        _repository.InsertAsync(
            request.Names, request.Surnames, request.BirthDate.ToDateTime(), request.SexCode,
            request.Phone, request.AlternativePhone, request.Email,
            request.Address, request.EmergencyContactName, request.EmergencyContactPhone,
            request.DocumentTypeCode, request.DocumentNumber)
            .MapErrorAsync(PersonError (error) => new PersonDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PersonDataAccessError("No se pudo registrar la persona."))
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<PersonResponse, PersonError>> UpdateAsync(Guid code, UpdatePersonRequest request) =>
        _repository.UpdateAsync(
            code, request.Names, request.Surnames, request.BirthDate.ToDateTime(), request.SexCode,
            request.Phone, request.AlternativePhone, request.Email,
            request.Address, request.EmergencyContactName, request.EmergencyContactPhone,
            request.DocumentTypeCode, request.DocumentNumber)
            .MapErrorAsync(PersonError (error) => new PersonDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PersonNotFoundError())
            .MapAsync(MapToResponse);

    private static PersonPageResponse BuildPageResponse(IEnumerable<PersonRow> rows, int page, int pageSize)
    {
        List<PersonRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PersonPageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static PersonResponse MapToResponse(PersonRow row) =>
        new()
        {
            PersonCode = row.PersonCode,
            Names = row.Names,
            Surnames = row.Surnames,
            BirthDate = row.BirthDate,
            SexCode = row.SexCode,
            SexName = row.SexName,
            Phone = row.Phone,
            AlternativePhone = row.AlternativePhone,
            Email = row.Email,
            Address = row.Address,
            EmergencyContactName = row.EmergencyContactName,
            EmergencyContactPhone = row.EmergencyContactPhone,
            DocumentTypeCode = row.DocumentTypeCode,
            DocumentTypeName = row.DocumentTypeName,
            DocumentNumber = row.DocumentNumber
        };
}
