using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.ConsultationType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public sealed class ConsultationTypeController : FunctionalController
{
    private readonly IConsultationType _consultationTypeService;
    private readonly IErrorHttpMapper<LookupError> _errorMapper;

    public ConsultationTypeController(
        IConsultationType consultationTypeService,
        IErrorHttpMapper<LookupError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _consultationTypeService = consultationTypeService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns all active consultation types in the catalog.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GuidLookupItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAllActive() =>
        ExecuteAsync(
            operation: () => _consultationTypeService.GetAllActiveAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetAllActive)
        );
}