using Application.Core.DTOs.PartnerCatalog.Errors;
using Application.Core.DTOs.PartnerCatalog.Request;
using Application.Core.DTOs.PartnerCatalog.Response;
using Application.Core.Interfaces.PartnerCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

/// <summary>The partner's own switches and prices over shared-catalog items. The catalog itself is read-only here.</summary>
[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/partner")]
public sealed class PartnerCatalogController : FunctionalController
{
    private readonly IPartnerCatalog _service;
    private readonly IErrorHttpMapper<PartnerCatalogError> _errorMapper;

    public PartnerCatalogController(
        IPartnerCatalog service,
        IErrorHttpMapper<PartnerCatalogError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _service = service;
        _errorMapper = errorMapper;
    }

    /// <summary>Enables/disables a catalog medication for this partner and/or sets its reorder level.</summary>
    [HttpPatch("medications/{code:guid}")]
    [ProducesResponseType(typeof(PartnerMedicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateMedication(Guid code, [FromBody] UpdatePartnerMedicationRequest request) =>
        ExecuteAsync(
            operation: () => _service.UpdateMedicationAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(UpdateMedication)
        );

    /// <summary>Enables/disables a catalog specialty for this partner and/or sets its prices.</summary>
    [HttpPatch("specialties/{code:guid}")]
    [ProducesResponseType(typeof(PartnerSpecialtyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateSpecialty(Guid code, [FromBody] UpdatePartnerSpecialtyRequest request) =>
        ExecuteAsync(
            operation: () => _service.UpdateSpecialtyAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(UpdateSpecialty)
        );

    /// <summary>Enables/disables a catalog insurer for this partner and/or overrides its coverage percentage.</summary>
    [HttpPatch("insurances/{code:guid}")]
    [ProducesResponseType(typeof(PartnerInsuranceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateInsurance(Guid code, [FromBody] UpdatePartnerInsuranceRequest request) =>
        ExecuteAsync(
            operation: () => _service.UpdateInsuranceAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(UpdateInsurance)
        );
}
