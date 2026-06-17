using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.Lookup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class LookupsController : FunctionalController
{
    private readonly ILookup _lookupService;
    private readonly IErrorHttpMapper<LookupError> _errorMapper;

    public LookupsController(
        ILookup lookupService,
        IErrorHttpMapper<LookupError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _lookupService = lookupService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns pharmaceutical forms and administration routes used by the medication catalog.
    /// </summary>
    [Authorize(Roles = "Administrador,Doctor")]
    [HttpGet("medications")]
    [ProducesResponseType(typeof(MedicationLookupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMedicationLookups() =>
        ExecuteAsync(
            operation: () => _lookupService.GetMedicationLookupsAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetMedicationLookups)
        );

    /// <summary>
    /// Returns sexes, document types, roles and active specialties used by the user form.
    /// </summary>
    [Authorize(Roles = "Administrador,Doctor")]
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserLookupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetUserLookups() =>
        ExecuteAsync(
            operation: () => _lookupService.GetUserLookupsAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetUserLookups)
        );

    /// <summary>
    /// Returns allergy severities used by the patient form.
    /// </summary>
    [Authorize(Roles = "Administrador,Doctor")]
    [HttpGet("patients")]
    [ProducesResponseType(typeof(PatientLookupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPatientLookups() =>
        ExecuteAsync(
            operation: () => _lookupService.GetPatientLookupsAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetPatientLookups)
        );

    /// <summary>
    /// Returns prescription statuses, medications, administration routes and frequencies for the prescription form.
    /// </summary>
    [Authorize(Roles = "Doctor,Enfermero")]
    [HttpGet("prescriptions")]
    [ProducesResponseType(typeof(PrescriptionLookupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPrescriptionLookups() =>
        ExecuteAsync(
            operation: () => _lookupService.GetPrescriptionLookupsAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetPrescriptionLookups)
        );

    /// <summary>Returns consultation types for the appointment booking form.</summary>
    [Authorize(Roles = "Administrador")]
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(AppointmentLookupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAppointmentLookups() =>
        ExecuteAsync(
            operation: () => _lookupService.GetAppointmentLookupsAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetAppointmentLookups)
        );
}