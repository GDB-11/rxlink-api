using Application.Core.DTOs.Role.Errors;
using Application.Core.DTOs.Role.Request;
using Application.Core.DTOs.Role.Response;
using Application.Core.Interfaces.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public sealed class RoleController : FunctionalController
{
    private readonly IRole _roleService;
    private readonly IErrorHttpMapper<RoleError> _errorMapper;

    public RoleController(
        IRole roleService,
        IErrorHttpMapper<RoleError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _roleService = roleService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns a paginated list of roles. Supports optional text search on name.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(RolePageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage([FromQuery] RolePageRequest request) =>
        ExecuteAsync(
            operation: () => _roleService.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>
    /// Registers a new role in the catalog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateRoleRequest request) =>
        ExecuteAsync(
            operation: () => _roleService.CreateAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: role => Created($"api/roles/{role.RoleCode}", role)
        );

    /// <summary>
    /// Updates an existing active role identified by its code.
    /// </summary>
    [HttpPut("{code:guid}")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid code, [FromBody] UpdateRoleRequest request) =>
        ExecuteAsync(
            operation: () => _roleService.UpdateAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(Update)
        );

    /// <summary>
    /// Deactivates a role (soft-delete). The record is preserved to maintain FK integrity.
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
            operation: userCode => _roleService.DeactivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Deactivate),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Activates a previously deactivated role.
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
            operation: userCode => _roleService.ActivateAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Activate),
            successMapper: _ => NoContent()
        );
}