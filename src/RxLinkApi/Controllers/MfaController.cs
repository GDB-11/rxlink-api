using System.Security.Claims;
using Application.Core.DTOs.Mfa.Errors;
using Application.Core.DTOs.Mfa.Request;
using Application.Core.DTOs.Mfa.Response;
using Application.Core.Interfaces.Mfa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador,Doctor,Enfermero")]
[ApiController]
[Route("api/[controller]")]
public sealed class MfaController : FunctionalController
{
    private readonly IMfa _mfaService;
    private readonly IErrorHttpMapper<MfaError> _errorMapper;

    public MfaController(
        IMfa mfaService,
        IErrorHttpMapper<MfaError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _mfaService = mfaService;
        _errorMapper = errorMapper;
    }

    /// <summary>Returns whether the authenticated user has MFA enabled or an enrollment pending.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(MfaStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetStatus() =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _mfaService.GetStatusAsync(userCode),
            errorMapper: _errorMapper,
            operationName: nameof(GetStatus)
        );

    /// <summary>
    /// Starts MFA enrollment for the authenticated user and returns the secret / otpauth URI for an authenticator app.
    /// MFA is not enforced until <c>confirm</c> succeeds.
    /// </summary>
    [HttpPost("enroll")]
    [ProducesResponseType(typeof(MfaEnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Enroll() =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _mfaService.EnrollAsync(userCode,
                User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value ?? userCode.ToString()),
            errorMapper: _errorMapper,
            operationName: nameof(Enroll)
        );

    /// <summary>Confirms the pending enrollment with a code from the authenticator app.</summary>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Confirm([FromBody] ConfirmMfaRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _mfaService.ConfirmAsync(userCode, request),
            errorMapper: _errorMapper,
            operationName: nameof(Confirm),
            successMapper: _ => NoContent()
        );

    /// <summary>Resets a user's MFA (lost device). Administrador only.</summary>
    [Authorize(Roles = "Administrador")]
    [HttpDelete("users/{userCode:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Reset(Guid userCode) =>
        ExecuteAsync(
            operation: () => _mfaService.ResetAsync(userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Reset),
            successMapper: _ => NoContent()
        );
}
