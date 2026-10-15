using Application.Core.DTOs.Amendment.Errors;
using Application.Core.DTOs.Amendment.Request;
using Application.Core.DTOs.Amendment.Response;
using Application.Core.Interfaces.Amendment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

/// <summary>Clinician review queue for patients' amendment requests (45 CFR 164.526).</summary>
[Authorize(Roles = "Doctor")]
[ApiController]
[Route("api/amendment-requests")]
public sealed class AmendmentRequestController : FunctionalController
{
    private readonly IAmendment _amendments;
    private readonly IErrorHttpMapper<AmendmentError> _errorMapper;

    public AmendmentRequestController(IAmendment amendments, IErrorHttpMapper<AmendmentError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _amendments = amendments;
        _errorMapper = errorMapper;
    }

    /// <summary>Requests of every patient, pending first. Optional <c>status</c> filter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AmendmentPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] AmendmentPageRequest request) =>
        ExecuteAsync(
            operation: () => _amendments.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>Accepts or denies a pending request. The decision is final and always carries a reason.</summary>
    [HttpPost("{code:guid}/decision")]
    [ProducesResponseType(typeof(AmendmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Decide(Guid code, [FromBody] DecideAmendmentRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _amendments.DecideAsync(code, request, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Decide)
        );
}
