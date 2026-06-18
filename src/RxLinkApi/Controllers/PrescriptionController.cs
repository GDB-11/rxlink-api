using System.Security.Claims;
using Application.Core.DTOs.Prescription.Errors;
using Application.Core.DTOs.Prescription.Request;
using Application.Core.DTOs.Prescription.Response;
using Application.Core.Interfaces.Prescription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public sealed class PrescriptionController : FunctionalController
{
    private readonly IPrescription _prescriptionService;
    private readonly IErrorHttpMapper<PrescriptionError> _errorMapper;

    public PrescriptionController(
        IPrescription prescriptionService,
        IErrorHttpMapper<PrescriptionError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _prescriptionService = prescriptionService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Creates a new prescription (initial status: Borrador).
    /// </summary>
    [HttpPost("prescription")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreatePrescriptionRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _prescriptionService.CreateAsync(request, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: prescription => Created($"api/prescription/{prescription.PrescriptionCode}", prescription)
        );

    /// <summary>
    /// Returns the full prescription with its detail lines.
    /// </summary>
    [HttpGet("prescription/{code:guid}")]
    [Authorize(Roles = "Doctor,Enfermero")]
    [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetByCode(Guid code) =>
        ExecuteAsync(
            operation: () => _prescriptionService.GetAsync(code),
            errorMapper: _errorMapper,
            operationName: nameof(GetByCode)
        );
    
    /// <summary>
    /// Returns the full prescription of a patient with its detail lines.
    /// </summary>
    [HttpGet("prescription/patient/{code:guid}")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetByCodeFromPatient(Guid code) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _prescriptionService.GetForPatientAsync(code, patientCode),
            errorMapper: _errorMapper,
            operationName: nameof(GetByCode)
        );

    /// <summary>
    /// Updates notes, validUntil and lines. Only allowed when status is Borrador.
    /// </summary>
    [HttpPut("prescription/{code:guid}")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(PrescriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdatePrescriptionRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _prescriptionService.UpdateAsync(code, request, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Transitions Borrador → Activo.
    /// </summary>
    [HttpPatch("prescription/{code:guid}/sign")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Sign(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _prescriptionService.SignAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Sign),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Transitions Activo → Suspendido.
    /// </summary>
    [HttpPatch("prescription/{code:guid}/suspend")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Suspend(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _prescriptionService.SuspendAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Suspend),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Transitions Suspendido → Activo.
    /// </summary>
    [HttpPatch("prescription/{code:guid}/reactivate")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Reactivate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _prescriptionService.ReactivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Reactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Transitions any non-terminal status → Cancelado.
    /// </summary>
    [HttpPatch("prescription/{code:guid}/cancel")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Cancel(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _prescriptionService.CancelAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Cancel),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Transitions Activo → Dispensado.
    /// </summary>
    [HttpPatch("prescription/{code:guid}/dispense")]
    [Authorize(Roles = "Enfermero")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Dispense(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _prescriptionService.DispenseAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Dispense),
            successMapper: _ => NoContent()
        );
}