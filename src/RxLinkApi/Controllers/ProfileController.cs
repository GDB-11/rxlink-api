using Application.Core.DTOs.User.Errors;
using Application.Core.DTOs.User.Request;
using Application.Core.DTOs.User.Response;
using Application.Core.Interfaces.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ProfileController : FunctionalController
{
    private readonly IUser _userService;
    private readonly IErrorHttpMapper<UserError> _errorMapper;

    public ProfileController(
        IUser userService,
        IErrorHttpMapper<UserError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _userService = userService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns the full profile of the currently authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMyProfile() =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _userService.GetMyProfileAsync(userCode),
            errorMapper: _errorMapper,
            operationName: nameof(GetMyProfile)
        );

    /// <summary>
    /// Changes the password of the currently authenticated user.
    /// Requires the current password for verification.
    /// </summary>
    [HttpPatch("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _userService.ChangePasswordAsync(userCode, request),
            errorMapper: _errorMapper,
            operationName: nameof(ChangePassword),
            successMapper: _ => NoContent()
        );
}