using Application.Core.DTOs.Person.Errors;
using Application.Core.DTOs.Person.Request;
using Application.Core.DTOs.Person.Response;
using BindSharp;

namespace Application.Core.Interfaces.Person;

public interface IPerson
{
    /// <summary>Returns a single person by code, or <see cref="PersonNotFoundError"/> if it does not exist.</summary>
    Task<Result<PersonResponse, PersonError>> GetByCodeAsync(Guid code);

    /// <summary>Returns a paginated list of persons. Supports optional text search on names or surnames.</summary>
    Task<Result<PersonPageResponse, PersonError>> GetPageAsync(PersonPageRequest request);

    /// <summary>
    /// Returns persons available for linking (picker use). Optionally excludes those already
    /// linked to a User or Patient record.
    /// </summary>
    Task<Result<PersonPageResponse, PersonError>> GetAvailableAsync(PersonAvailableRequest request);

    /// <summary>Registers a new person.</summary>
    Task<Result<PersonResponse, PersonError>> CreateAsync(CreatePersonRequest request);

    /// <summary>Updates an existing person identified by its code.</summary>
    Task<Result<PersonResponse, PersonError>> UpdateAsync(Guid code, UpdatePersonRequest request);
}