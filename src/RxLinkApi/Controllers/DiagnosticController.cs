using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Diagnostic.Request;
using Application.Core.DTOs.Diagnostic.Response;
using Application.Core.Interfaces.Diagnostic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Doctor,Enfermero")]
[ApiController]
[Route("api")]
public sealed class DiagnosticController : FunctionalController
{
    private readonly IDiagnostic _diagnosticService;
    private readonly IErrorHttpMapper<DiagnosticError> _errorMapper;

    public DiagnosticController(
        IDiagnostic diagnosticService,
        IErrorHttpMapper<DiagnosticError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _diagnosticService = diagnosticService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of diagnostics for a patient (includes prescription summary if any).
    /// </summary>
    [HttpGet("patient/{patientCode:guid}/diagnostics")]
    [ProducesResponseType(typeof(DiagnosticPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage(Guid patientCode, [FromQuery] DiagnosticPageRequest request) =>
        ExecuteAsync(
            operation: () => _diagnosticService.GetPageAsync(patientCode, request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Creates a new diagnostic (initial status: Activo).
    /// </summary>
    [HttpPost("diagnostic")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(DiagnosticResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateDiagnosticRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _diagnosticService.CreateAsync(request, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: diag => Created($"api/diagnostic/{diag.DiagnosticCode}", diag)
        );

    /// <summary>
    /// Updates description, date and notes of an existing diagnostic.
    /// </summary>
    [HttpPut("diagnostic/{code:guid}")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(DiagnosticResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdateDiagnosticRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _diagnosticService.UpdateAsync(code, request, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Transitions a diagnostic from Activo to Inactivo.
    /// </summary>
    [HttpPatch("diagnostic/{code:guid}/deactivate")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Deactivate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _diagnosticService.DeactivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Transitions a diagnostic from Inactivo to Activo.
    /// </summary>
    [HttpPatch("diagnostic/{code:guid}/activate")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Activate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _diagnosticService.ActivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Activate),
            successMapper: _ => NoContent()
        );
}
