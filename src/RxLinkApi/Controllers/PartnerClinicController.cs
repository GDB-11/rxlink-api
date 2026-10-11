using Application.Core.DTOs.Clinic.Errors;
using Application.Core.DTOs.Clinic.Request;
using Application.Core.DTOs.Clinic.Response;
using Application.Core.Interfaces.Clinic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/partner/clinic")]
public sealed class PartnerClinicController : FunctionalController
{
    private readonly IClinic _clinicService;
    private readonly IErrorHttpMapper<ClinicError> _errorMapper;

    public PartnerClinicController(
        IClinic clinicService,
        IErrorHttpMapper<ClinicError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _clinicService = clinicService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns the partner's default clinic (operational data).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ClinicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Get() =>
        ExecuteAsync(
            operation: () => _clinicService.GetDefaultAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(Get)
        );

    /// <summary>
    /// Updates the operational fields (address, contact, facility registration code) of the default clinic.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ClinicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update([FromBody] UpdateClinicRequest request) =>
        ExecuteAsync(
            operation: () => _clinicService.UpdateDefaultAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );
}
