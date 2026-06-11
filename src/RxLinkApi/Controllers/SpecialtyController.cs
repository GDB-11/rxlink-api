using Application.Core.DTOs.Specialty.Errors;
using Application.Core.DTOs.Specialty.Request;
using Application.Core.DTOs.Specialty.Response;
using Application.Core.Interfaces.Specialty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public sealed class SpecialtyController : FunctionalController
{
    private readonly ISpecialty _specialtyService;
    private readonly IErrorHttpMapper<SpecialtyError> _errorMapper;

    public SpecialtyController(
        ISpecialty specialtyService,
        IErrorHttpMapper<SpecialtyError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _specialtyService = specialtyService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of specialties. 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SpecialtyPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] SpecialtyPageRequest request) =>
        ExecuteAsync(
            operation: () => _specialtyService.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Registers a new specialty in the catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SpecialtyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateSpecialtyRequest request) =>
        ExecuteAsync(
            operation: () => _specialtyService.CreateAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: specialty => Created($"api/especialidades/{specialty.SpecialtyCode}", specialty)
        );

    /// <summary>
    /// Updates an existing active specialty identified by its code.
    /// </summary>
    [HttpPut("{code:guid}")]
    [ProducesResponseType(typeof(SpecialtyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdateSpecialtyRequest request) =>
        ExecuteAsync(
            operation: () => _specialtyService.UpdateAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Deactivates a specialty (soft-delete). The record is preserved to maintain FK integrity.
    /// </summary>
    [HttpPatch("{code:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Deactivate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _specialtyService.DeactivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Activates a specialty.
    /// </summary>
    [HttpPatch("{code:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Activate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _specialtyService.ActivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Activate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Returns all active specialties with their doctor count. Public endpoint — no authentication required.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/api/specialties")]
    [ProducesResponseType(typeof(IEnumerable<SpecialtyWithDoctorCountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAllActive() =>
        ExecuteAsync(
            operation: () => _specialtyService.GetAllActiveWithDoctorCountAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetAllActive)
        );

    /// <summary>
    /// Returns active doctors assigned to the given specialty. Public endpoint — no authentication required.
    /// Returns 404 when the specialty does not exist or is inactive.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{code:guid}/doctors")]
    [ProducesResponseType(typeof(IEnumerable<DoctorSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetDoctorsBySpecialty(Guid code) =>
        ExecuteAsync(
            operation: () => _specialtyService.GetDoctorsBySpecialtyCodeAsync(code),
            errorMapper: _errorMapper,
            operationName: nameof(GetDoctorsBySpecialty)
        );
}