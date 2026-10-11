using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.DTOs.PatientAccess.Request;
using Application.Core.DTOs.PatientAccess.Response;
using Application.Core.DTOs.Report.Request;
using Application.Core.Interfaces.PatientAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public sealed class BreakGlassController : FunctionalController
{
    private readonly IBreakGlass _breakGlassService;
    private readonly IErrorHttpMapper<PatientAccessError> _errorMapper;

    public BreakGlassController(
        IBreakGlass breakGlassService,
        IErrorHttpMapper<PatientAccessError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _breakGlassService = breakGlassService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Opens a patient outside the nurse's care team for a limited time. Requires the exact medical record
    /// number and a reason; the grant is logged and surfaced to the administrator.
    /// </summary>
    [HttpPost("nurse/break-glass")]
    [Authorize(Roles = "Enfermero")]
    [ProducesResponseType(typeof(BreakGlassResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Request([FromBody] BreakGlassRequest request) =>
        ExecuteAsync(
            operation: () => _breakGlassService.RequestAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Request)
        );

    /// <summary>
    /// Break-glass grants over a mandatory date range (who, which patient, why, until when).
    /// </summary>
    [HttpGet("report/break-glass")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(IReadOnlyList<BreakGlassReportItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetReport([FromQuery] ReportDateRangeRequest request) =>
        ExecuteAsync(
            operation: () => _breakGlassService.GetReportAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetReport)
        );
}
