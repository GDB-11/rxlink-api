using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Catalog.Response;
using Application.Core.DTOs.Allergy.Request;
using Application.Core.DTOs.Allergy.Response;
using Application.Core.Interfaces.Catalog;
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
    private readonly ICatalogView _catalogView;
    private readonly IErrorHttpMapper<CatalogReadError> _catalogReadErrorMapper;

    public AllergyController(
        ICatalogView catalogView,
        IErrorHttpMapper<CatalogReadError> catalogReadErrorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _catalogView = catalogView;
        _catalogReadErrorMapper = catalogReadErrorMapper;
    }

    /// <summary>
    /// Returns a paginated list of allergies. Supports optional text search on name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CatalogPageResponse<AllergyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] AllergyPageRequest request) =>
        ExecuteAsync(
            operation: () => _catalogView.GetAllergiesPageAsync(request),
            errorMapper: _catalogReadErrorMapper,
            operationName: nameof(GetPage)
        );
}
