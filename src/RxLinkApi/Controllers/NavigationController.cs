using Application.Core.DTOs.Navigation.Errors;
using Application.Core.DTOs.Navigation.Response;
using Application.Core.Interfaces.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class NavigationController : FunctionalController
{
    private readonly INavigation _navigationService;
    private readonly IErrorHttpMapper<NavigationError> _errorMapper;

    public NavigationController(
        INavigation navigationService,
        IErrorHttpMapper<NavigationError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _navigationService = navigationService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Returns the navigation tree (topbar modules + sidebar items) for the authenticated user's role.
    /// </summary>
    /// <returns>Navigation modules and their items ordered by display position.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(NavigationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetNavigation() =>
        ExecuteWithRoleAsync(
            operation: roleName => _navigationService.GetNavigationAsync(roleName),
            errorMapper: _errorMapper,
            operationName: nameof(GetNavigation)
        );
}