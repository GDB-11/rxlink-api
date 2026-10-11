using Application.Core.DTOs.Report.Errors;
using Application.Core.DTOs.Report.Request;
using Application.Core.DTOs.Report.Response;
using Application.Core.Interfaces.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public sealed class ReportController : FunctionalController
{
    private readonly IReport _reportService;
    private readonly IErrorHttpMapper<ReportError> _errorMapper;

    public ReportController(
        IReport reportService,
        IErrorHttpMapper<ReportError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _reportService = reportService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Event counts per user and event type over a mandatory date range (includes the patient-record access log).
    /// </summary>
    [HttpGet("activity-by-user")]
    [ProducesResponseType(typeof(IReadOnlyList<ActivityByUserItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetActivityByUser([FromQuery] ReportDateRangeRequest request) =>
        ExecuteAsync(
            operation: () => _reportService.GetActivityByUserAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetActivityByUser)
        );

    /// <summary>
    /// Audited writes that landed with no actor (missed audit context) over a mandatory date range.
    /// </summary>
    [HttpGet("unattributed-writes")]
    [ProducesResponseType(typeof(IReadOnlyList<UnattributedWriteItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetUnattributedWrites([FromQuery] ReportDateRangeRequest request) =>
        ExecuteAsync(
            operation: () => _reportService.GetUnattributedWritesAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetUnattributedWrites)
        );
}
