using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Catalog.Response;
using Application.Core.DTOs.Specialty.Errors;
using Application.Core.DTOs.Specialty.Request;
using Application.Core.DTOs.Specialty.Response;
using Application.Core.Interfaces.Specialty;
using Application.Core.Interfaces.Catalog;
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
    private readonly ICatalogView _catalogView;
    private readonly IErrorHttpMapper<CatalogReadError> _catalogReadErrorMapper;

    public SpecialtyController(
        ISpecialty specialtyService,
        IErrorHttpMapper<SpecialtyError> errorMapper,
        ICatalogView catalogView,
        IErrorHttpMapper<CatalogReadError> catalogReadErrorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _specialtyService = specialtyService;
        _errorMapper = errorMapper;
        _catalogView = catalogView;
        _catalogReadErrorMapper = catalogReadErrorMapper;
    }

    /// <summary>
    /// Returns a paginated list of specialties. 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CatalogPageResponse<SpecialtyCatalogItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] SpecialtyPageRequest request) =>
        ExecuteAsync(
            operation: () => _catalogView.GetSpecialtiesPageAsync(request),
            errorMapper: _catalogReadErrorMapper,
            operationName: nameof(GetPage)
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
    public Task<IActionResult> GetDoctorsBySpecialty(Guid code, [FromQuery] string? search = null) =>
        ExecuteAsync(
            operation: () => _specialtyService.GetDoctorsBySpecialtyCodeAsync(code, search),
            errorMapper: _errorMapper,
            operationName: nameof(GetDoctorsBySpecialty)
        );
}