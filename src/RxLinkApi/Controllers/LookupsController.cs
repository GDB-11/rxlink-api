using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.Lookup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador,Doctor")]
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
        _errorMapper   = errorMapper;
    }

    /// <summary>
    /// Returns pharmaceutical forms and administration routes used by the medication catalog.
    /// </summary>
    [HttpGet("medications")]
    [ProducesResponseType(typeof(MedicationLookupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMedicationLookups() =>
        ExecuteAsync(
            operation:     () => _lookupService.GetMedicationLookupsAsync(),
            errorMapper:   _errorMapper,
            operationName: nameof(GetMedicationLookups)
        );

    /// <summary>
    /// Returns sexes, document types, roles and active specialties used by the user form.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserLookupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetUserLookups() =>
        ExecuteAsync(
            operation:     () => _lookupService.GetUserLookupsAsync(),
            errorMapper:   _errorMapper,
            operationName: nameof(GetUserLookups)
        );

    /// <summary>
    /// Returns allergy severities used by the patient form.
    /// </summary>
    [HttpGet("patients")]
    [ProducesResponseType(typeof(PatientLookupsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPatientLookups() =>
        ExecuteAsync(
            operation:     () => _lookupService.GetPatientLookupsAsync(),
            errorMapper:   _errorMapper,
            operationName: nameof(GetPatientLookups)
        );
}
