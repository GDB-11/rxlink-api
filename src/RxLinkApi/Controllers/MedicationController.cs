using Application.Core.DTOs.Medication.Errors;
using Application.Core.DTOs.Medication.Request;
using Application.Core.DTOs.Medication.Response;
using Application.Core.Interfaces.Medication;
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
    private readonly IMedication _medicationService;
    private readonly IErrorHttpMapper<MedicationError> _errorMapper;

    public MedicationController(
        IMedication medicationService,
        IErrorHttpMapper<MedicationError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _medicationService = medicationService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of medications. Supports optional text search on generic or commercial name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MedicationPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] MedicationPageRequest request) =>
        ExecuteAsync(
            operation: () => _medicationService.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Registers a new medication in the catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MedicationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateMedicationRequest request) =>
        ExecuteAsync(
            operation: () => _medicationService.CreateAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: medication => Created($"api/medicamento/{medication.MedicationCode}", medication)
        );

    /// <summary>
    /// Updates an existing active medication identified by its code.
    /// </summary>
    [HttpPut("{code:guid}")]
    [ProducesResponseType(typeof(MedicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdateMedicationRequest request) =>
        ExecuteAsync(
            operation: () => _medicationService.UpdateAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Deactivates a medication (soft-delete). The record is preserved to maintain FK integrity.
    /// </summary>
    [HttpPatch("{code:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Deactivate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _medicationService.DeactivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Activates a medication.
    /// </summary>
    [HttpPatch("{code:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Activate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _medicationService.ActivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );
}