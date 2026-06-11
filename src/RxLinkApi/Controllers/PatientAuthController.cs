using Application.Core.DTOs.PatientAuth.Errors;
using Application.Core.DTOs.PatientAuth.Request;
using Application.Core.DTOs.PatientAuth.Response;
using Application.Core.Interfaces.PatientAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[ApiController]
[Route("api/patient-auth")]
public sealed class PatientAuthController : FunctionalController
{
    private readonly IPatientAuthentication _service;
    private readonly IErrorHttpMapper<PatientAuthError> _errorMapper;

    public PatientAuthController(
        IPatientAuthentication service,
        IErrorHttpMapper<PatientAuthError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _service = service;
        _errorMapper = errorMapper;
    }

    [AllowAnonymous]
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(PatientLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Lookup(
        [FromQuery] Guid documentTypeCode,
        [FromQuery] string documentNumber) =>
        ExecuteAsync(
            operation: () => _service.LookupAsync(documentTypeCode, documentNumber),
            errorMapper: _errorMapper,
            operationName: nameof(Lookup)
        );

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(PatientAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Register([FromBody] RegisterPatientRequest request) =>
        ExecuteAsync(
            operation: () => _service.RegisterAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Register)
        );

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(PatientAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Login([FromBody] PatientLoginRequest request) =>
        ExecuteAsync(
            operation: () => _service.LoginAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Login)
        );

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(PatientAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Refresh([FromBody] PatientRefreshRequest request) =>
        ExecuteAsync(
            operation: () => _service.RefreshAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Refresh)
        );

    [Authorize(Roles = "Patient")]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Logout() =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _service.LogoutAsync(patientCode),
            errorMapper: _errorMapper,
            operationName: nameof(Logout),
            successMapper: _ => NoContent()
        );
}