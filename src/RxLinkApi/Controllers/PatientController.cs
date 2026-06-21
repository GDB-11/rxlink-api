using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Patient.Request;
using Application.Core.DTOs.Patient.Response;
using Application.Core.Interfaces.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador,Doctor,Enfermero")]
[ApiController]
[Route("api/[controller]")]
public sealed class PatientController : FunctionalController
{
    private readonly IPatient _patientService;
    private readonly IErrorHttpMapper<PatientError> _errorMapper;

    public PatientController(
        IPatient patientService,
        IErrorHttpMapper<PatientError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _patientService = patientService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of patients. Supports optional text search on names or surnames.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PatientPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] PatientPageRequest request) =>
        ExecuteAsync(
            operation: () => _patientService.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Returns the patient linked to a given person code, including allergies.
    /// </summary>
    [HttpGet("by-person/{personCode:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetByPersonCode(Guid personCode) =>
        ExecuteAsync(
            operation: () => _patientService.GetByPersonCodeAsync(personCode),
            errorMapper: _errorMapper,
            operationName: nameof(GetByPersonCode)
        );

    /// <summary>
    /// Returns a single patient by code, including allergies.
    /// </summary>
    [HttpGet("{code:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetByCode(Guid code) =>
        ExecuteAsync(
            operation: () => _patientService.GetSelfAsync(code),
            errorMapper: _errorMapper,
            operationName: nameof(GetByCode)
        );

    /// <summary>
    /// Registers a new patient.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreatePatientRequest request) =>
        ExecuteAsync(
            operation: () => _patientService.CreateAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: patient => Created($"api/patient/{patient.PatientCode}", patient)
        );

    /// <summary>
    /// Updates an existing active patient identified by its code.
    /// </summary>
    [HttpPut("{code:guid}")]
    [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdatePatientRequest request) =>
        ExecuteAsync(
            operation: () => _patientService.UpdateAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Deactivates a patient (soft-delete). The record is preserved to maintain FK integrity.
    /// </summary>
    [HttpPatch("{code:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Deactivate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _patientService.DeactivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Activates a patient.
    /// </summary>
    [HttpPatch("{code:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Activate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _patientService.ActivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Activate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Adds an allergy to an existing patient.
    /// </summary>
    [HttpPost("{code:guid}/allergies")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(PatientAllergyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> AddAllergy(Guid code, [FromBody] PatientAllergyRequest request) =>
        ExecuteAsync(
            operation: () => _patientService.AddAllergyAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(AddAllergy),
            successMapper: allergy => Created($"api/patient/{code}/allergies/{allergy.PatientAllergyCode}", allergy)
        );

    /// <summary>
    /// Updates the severity and notes of an existing patient allergy.
    /// </summary>
    [HttpPut("{code:guid}/allergies/{allergyCode:guid}")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(typeof(PatientAllergyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateAllergy(Guid code, Guid allergyCode, [FromBody] PatientAllergyRequest request) =>
        ExecuteAsync(
            operation: () => _patientService.UpdateAllergyAsync(code, allergyCode, request),
            errorMapper: _errorMapper,
            operationName: nameof(UpdateAllergy)
        );

    /// <summary>
    /// Removes an allergy from a patient (soft-delete).
    /// </summary>
    [HttpDelete("{code:guid}/allergies/{allergyCode:guid}")]
    [Authorize(Roles = "Administrador,Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> DeleteAllergy(Guid code, Guid allergyCode) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _patientService.RemoveAllergyAsync(code, allergyCode, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(DeleteAllergy),
            successMapper: _ => NoContent()
        );
}