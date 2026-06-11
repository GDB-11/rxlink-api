using Application.Core.DTOs.Person.Errors;
using Application.Core.DTOs.Person.Request;
using Application.Core.DTOs.Person.Response;
using Application.Core.Interfaces.Person;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador,Doctor,Enfermero")]
[ApiController]
[Route("api/[controller]")]
public sealed class PersonController : FunctionalController
{
    private readonly IPerson _personService;
    private readonly IErrorHttpMapper<PersonError> _errorMapper;

    public PersonController(
        IPerson personService,
        IErrorHttpMapper<PersonError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _personService = personService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of persons. Supports optional text search on names or surnames.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PersonPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] PersonPageRequest request) =>
        ExecuteAsync(
            operation: () => _personService.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Returns persons available for linking. Optionally excludes persons already linked to
    /// a User or Patient record. Intended for picker/autocomplete use.
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(PersonPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAvailable([FromQuery] PersonAvailableRequest request) =>
        ExecuteAsync(
            operation: () => _personService.GetAvailableAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetAvailable)
        );

    /// <summary>
    /// Registers a new person.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PersonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreatePersonRequest request) =>
        ExecuteAsync(
            operation: () => _personService.CreateAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: person => Created($"api/person/{person.PersonCode}", person)
        );

    /// <summary>
    /// Updates an existing person identified by its code.
    /// </summary>
    [HttpPut("{code:guid}")]
    [ProducesResponseType(typeof(PersonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdatePersonRequest request) =>
        ExecuteAsync(
            operation: () => _personService.UpdateAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );
}