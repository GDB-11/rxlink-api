using Application.Core.DTOs.Allergy.Errors;
using Application.Core.DTOs.Allergy.Request;
using Application.Core.DTOs.Allergy.Response;
using Application.Core.Interfaces.Allergy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador,Doctor")]
[ApiController]
[Route("api/[controller]")]
public sealed class AllergyController : FunctionalController
{
    private readonly IAllergy _allergyService;
    private readonly IErrorHttpMapper<AllergyError> _errorMapper;

    public AllergyController(
        IAllergy allergyService,
        IErrorHttpMapper<AllergyError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _allergyService = allergyService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of allergies. Supports optional text search on name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AllergyPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] AllergyPageRequest request) =>
        ExecuteAsync(
            operation: () => _allergyService.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Registers a new allergy in the catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AllergyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateAllergyRequest request) =>
        ExecuteAsync(
            operation: () => _allergyService.CreateAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: allergy => Created($"api/allergy/{allergy.AllergyCode}", allergy)
        );

    /// <summary>
    /// Updates an existing active allergy identified by its code.
    /// </summary>
    [HttpPut("{code:guid}")]
    [ProducesResponseType(typeof(AllergyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdateAllergyRequest request) =>
        ExecuteAsync(
            operation: () => _allergyService.UpdateAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Deactivates an allergy (soft-delete). The record is preserved to maintain FK integrity.
    /// </summary>
    [HttpPatch("{code:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Deactivate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _allergyService.DeactivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Activates an allergy.
    /// </summary>
    [HttpPatch("{code:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Activate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _allergyService.ActivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );
}
