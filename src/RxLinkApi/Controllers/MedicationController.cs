using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Catalog.Response;
using Application.Core.DTOs.Medication.Request;
using Application.Core.DTOs.Medication.Response;
using Application.Core.Interfaces.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public sealed class MedicationController : FunctionalController
{
    private readonly ICatalogView _catalogView;
    private readonly IErrorHttpMapper<CatalogReadError> _catalogReadErrorMapper;

    public MedicationController(
        ICatalogView catalogView,
        IErrorHttpMapper<CatalogReadError> catalogReadErrorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _catalogView = catalogView;
        _catalogReadErrorMapper = catalogReadErrorMapper;
    }

    /// <summary>
    /// Returns a paginated list of medications. Supports optional text search on generic or commercial name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CatalogPageResponse<MedicationCatalogItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] MedicationPageRequest request) =>
        ExecuteAsync(
            operation: () => _catalogView.GetMedicationsPageAsync(request),
            errorMapper: _catalogReadErrorMapper,
            operationName: nameof(GetPage)
        );
}
