using Application.Core.DTOs.Insurance.Errors;
using Application.Core.DTOs.Insurance.Request;
using Application.Core.DTOs.Insurance.Response;
using Application.Core.Interfaces.Insurance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public sealed class InsuranceController : FunctionalController
{
    private readonly IInsurance _insuranceService;
    private readonly IErrorHttpMapper<InsuranceError> _errorMapper;

    public InsuranceController(
        IInsurance insuranceService,
        IErrorHttpMapper<InsuranceError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _insuranceService = insuranceService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of insurances.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(InsurancePageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] InsurancePageRequest request) =>
        ExecuteAsync(
            operation: () => _insuranceService.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Registers a new insurance in the catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InsuranceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateInsuranceRequest request) =>
        ExecuteAsync(
            operation: () => _insuranceService.CreateAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: insurance => Created($"api/insurance/{insurance.InsuranceCode}", insurance)
        );

    /// <summary>
    /// Updates an existing active insurance identified by its code.
    /// </summary>
    [HttpPut("{code:guid}")]
    [ProducesResponseType(typeof(InsuranceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdateInsuranceRequest request) =>
        ExecuteAsync(
            operation: () => _insuranceService.UpdateAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Deactivates an insurance (soft-delete). The record is preserved to maintain FK integrity.
    /// </summary>
    [HttpPatch("{code:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Deactivate(Guid code) =>
        ExecuteAsync(
            operation: () => _insuranceService.DeactivateAsync(code),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Activates an insurance.
    /// </summary>
    [HttpPatch("{code:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Activate(Guid code) =>
        ExecuteAsync(
            operation: () => _insuranceService.ActivateAsync(code),
            errorMapper: _errorMapper,
            operationName: nameof(Activate),
            successMapper: _ => NoContent()
        );
}
