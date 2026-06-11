using Application.Core.DTOs.User.Errors;
using Application.Core.DTOs.User.Request;
using Application.Core.DTOs.User.Response;
using Application.Core.Interfaces.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public sealed class UserController : FunctionalController
{
    private readonly IUser _userService;
    private readonly IErrorHttpMapper<UserError> _errorMapper;

    public UserController(
        IUser userService,
        IErrorHttpMapper<UserError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _userService = userService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of platform users. Supports optional text search on names, surnames, username or email.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] UserPageRequest request) =>
        ExecuteAsync(
            operation: () => _userService.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Creates a new platform user together with their person record and identity document.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateUserRequest request) =>
        ExecuteAsync(
            operation: () => _userService.CreateAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: user => Created($"api/usuarios/{user.UserCode}", user)
        );

    /// <summary>
    /// Updates the person data, identity document and account details of an existing active user.
    /// The password is not changed through this endpoint.
    /// </summary>
    [HttpPut("{code:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdateUserRequest request) =>
        ExecuteAsync(
            operation: () => _userService.UpdateAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Deactivates a platform user. The record is preserved to maintain FK integrity.
    /// The authenticated caller must be an active Administrador.
    /// </summary>
    [HttpPatch("{code:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Deactivate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _userService.DeactivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Activates a platform user. The record is preserved to maintain FK integrity.
    /// The authenticated caller must be an active Administrador.
    /// </summary>
    [HttpPatch("{code:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Activate(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _userService.ActivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Activate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Updates only the role of an existing active user.
    /// Specialty, username, email and licence are preserved.
    /// </summary>
    [HttpPatch("{code:guid}/role")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> UpdateRole(Guid code, [FromBody] UpdateUserRoleRequest request) =>
        ExecuteAsync(
            operation: () => _userService.UpdateRoleAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(UpdateRole)
        );
}