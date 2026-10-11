using Application.Core.Interfaces.Catalog;
using System.Text.Json;
using Application.Core.DTOs.Patient.Request;
using Application.Core.DTOs.Person.Errors;
using Application.Core.DTOs.Person.Request;
using Application.Core.DTOs.Person.Response;
using Application.Core.Interfaces.Auth;
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
    private readonly IPassword _passwordService;

    private readonly ICatalogWriteValidator _catalog;

    public PersonService(IPersonRepository repository, IPassword passwordService, ICatalogWriteValidator catalog)
    {
        _repository = repository;
        _passwordService = passwordService;
        _catalog = catalog;
    }

    /// <inheritdoc/>
    public Task<Result<PersonResponse, PersonError>> GetByCodeAsync(Guid code) =>
        _repository.GetByCodeAsync(code)
            .MapErrorAsync(PersonError (error) =>
                new PersonDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PersonNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<PersonPageResponse, PersonError>> GetPageAsync(PersonPageRequest request) =>
        _repository.GetPageAsync((request.Page - 1) * request.PageSize, request.PageSize, request.Search)
            .MapErrorAsync(PersonError (error) =>
                new PersonDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));

    /// <inheritdoc/>
    public Task<Result<PersonPageResponse, PersonError>> GetAvailableAsync(PersonAvailableRequest request) =>
        _repository.GetAvailableAsync(
                (request.Page - 1) * request.PageSize, request.PageSize, request.Search,
                request.ExcludeLinkedUsers, request.ExcludeLinkedPatients)
            .MapErrorAsync(PersonError (error) =>
                new PersonDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));

    /// <inheritdoc/>
    public Task<Result<PersonResponse, PersonError>> CreateAsync(CreatePersonRequest request)
    {
        string allergiesJson = JsonSerializer.Serialize(
            request.Allergies.Select(a => new
                { AllergyCode = a.AllergyCode, SeverityCode = a.SeverityCode, Notes = a.Notes }));

        return ValidateCatalogAsync(request.SexCode, request.DocumentTypeCode, request.Allergies)
            .BindAsync(_ => _passwordService
            .HashPassword(request.DocumentNumber)
            .MapError(PersonError (e) => new PersonDataAccessError(e.Message, null, e.Exception))
            .AsTask()
            .BindAsync(passwordHash => _repository.InsertAsync(
                    request.Names, request.Surnames, request.BirthDate.ToDateTime(), request.SexCode,
                    request.Phone, request.AlternativePhone, request.Email,
                    request.Address, request.EmergencyContactName, request.EmergencyContactPhone,
                    request.DocumentTypeCode, request.DocumentNumber, passwordHash, allergiesJson)
                .MapErrorAsync(PersonError (error) =>
                    new PersonDataAccessError(error.Message, error.Details, error.Exception)))
            .EnsureNotNullAsync(new PersonDataAccessError("No se pudo registrar la persona."))
            .MapAsync(MapToResponse));
    }

    private async Task<Result<Unit, PersonError>> ValidateCatalogAsync(
        Guid sexCode, Guid documentTypeCode, IEnumerable<PatientAllergyItem>? allergies)
    {
        Result<Unit, PersonError> result = await _catalog.ValidatePersonAsync(sexCode, documentTypeCode)
            .MapErrorAsync(PersonError (error) => new PersonCatalogValidationError(error));
        if (!result.IsSuccess || allergies is null) return result;

        return await _catalog.ValidateAllergiesAsync(allergies.Select(a => (a.AllergyCode, a.SeverityCode)).ToList())
            .MapErrorAsync(PersonError (error) => new PersonCatalogValidationError(error));
    }

    /// <inheritdoc/>
    public Task<Result<PersonResponse, PersonError>> UpdateAsync(Guid code, UpdatePersonRequest request) =>
        ValidateCatalogAsync(request.SexCode, request.DocumentTypeCode, null)
            .BindAsync(_ => _repository.UpdateAsync(
                code, request.Names, request.Surnames, request.BirthDate.ToDateTime(), request.SexCode,
                request.Phone, request.AlternativePhone, request.Email,
                request.Address, request.EmergencyContactName, request.EmergencyContactPhone,
                request.DocumentTypeCode, request.DocumentNumber)
            .MapErrorAsync(PersonError (error) =>
                new PersonDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PersonNotFoundError())
            .MapAsync(MapToResponse));

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