using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Auth.Request;
using Application.Core.DTOs.Auth.Response;
using Application.Core.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RxLinkApi.DependencyInjection;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : FunctionalController
{
    private readonly IAuthentication _authenticationService;
    private readonly IErrorHttpMapper<AuthenticationError> _errorMapper;

    public AuthController(
        IAuthentication authenticationService,
        IErrorHttpMapper<AuthenticationError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _authenticationService = authenticationService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT tokens and user information</returns>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingRegistration.AuthPolicy)]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Login([FromBody] LoginRequest request) =>
        ExecuteAsync(
            operation: () => _authenticationService.LoginAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Login)
        );

    /// <summary>
    /// Completes login for an MFA-enabled account with the token returned by <c>login</c> (202) and an authenticator code
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingRegistration.AuthPolicy)]
    [HttpPost("mfa/verify")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequest request) =>
        ExecuteAsync(
            operation: () => _authenticationService.VerifyMfaAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(VerifyMfa)
        );

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New JWT tokens and user information</returns>
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingRegistration.AuthPolicy)]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request) =>
        ExecuteAsync(
            operation: () => _authenticationService.RefreshTokenAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(RefreshToken)
        );

    /// <summary>
    /// Logs out a user by invalidating their refresh token
    /// </summary>
    /// <param name="request">Refresh token to invalidate</param>
    /// <returns>No content on success</returns>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request) =>
        await ExecuteAsync(
            operation: () => _authenticationService.LogoutAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Logout),
            successMapper: _ => NoContent()
        );
}