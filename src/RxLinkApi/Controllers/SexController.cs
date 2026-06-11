using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.Sex;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public sealed class SexController : FunctionalController
{
    private readonly ISex _sexService;
    private readonly IErrorHttpMapper<LookupError> _errorMapper;

    public SexController(
        ISex sexService,
        IErrorHttpMapper<LookupError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _sexService = sexService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns all sexes in the catalog.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GuidLookupItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAll() =>
        ExecuteAsync(
            operation: () => _sexService.GetAllAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetAll)
        );
}