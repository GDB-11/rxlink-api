using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Diagnostic.Request;
using Application.Core.DTOs.Diagnostic.Response;
using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Patient.Request;
using Application.Core.DTOs.Patient.Response;
using Application.Core.Interfaces.Diagnostic;
using Application.Core.Interfaces.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Patient")]
[ApiController]
[Route("api/patient")]
public sealed class PatientMedicalController : FunctionalController
{
    private readonly IDiagnostic _diagnosticService;
    private readonly IPatient _patientService;
    private readonly IErrorHttpMapper<DiagnosticError> _diagnosticErrorMapper;
    private readonly IErrorHttpMapper<PatientError> _patientErrorMapper;

    public PatientMedicalController(
        IDiagnostic diagnosticService,
        IPatient patientService,
        IErrorHttpMapper<DiagnosticError> diagnosticErrorMapper,
        IErrorHttpMapper<PatientError> patientErrorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _diagnosticService = diagnosticService;
        _patientService = patientService;
        _diagnosticErrorMapper = diagnosticErrorMapper;
        _patientErrorMapper = patientErrorMapper;
    }

    /// <summary>Returns the authenticated patient's diagnostics with prescription summary.</summary>
    [HttpGet("diagnostics")]
    [ProducesResponseType(typeof(DiagnosticPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetDiagnostics([FromQuery] DiagnosticPageRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _diagnosticService.GetPageAsync(patientCode, request),
            errorMapper: _diagnosticErrorMapper,
            operationName: nameof(GetDiagnostics)
        );

    /// <summary>Returns the authenticated patient's own profile including allergies.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMe() =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _patientService.GetSelfAsync(patientCode),
            errorMapper: _patientErrorMapper,
            operationName: nameof(GetMe)
        );

    /// <summary>Updates the authenticated patient's contact information.</summary>
    [HttpPatch("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateMe([FromBody] UpdatePatientSelfRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _patientService.UpdateSelfAsync(patientCode, request),
            errorMapper: _patientErrorMapper,
            operationName: nameof(UpdateMe),
            successMapper: _ => NoContent()
        );
}