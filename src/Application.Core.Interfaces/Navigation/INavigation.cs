using Application.Core.DTOs.Navigation.Errors;
using Application.Core.DTOs.Navigation.Response;
using BindSharp;

namespace Application.Core.Interfaces.Navigation;

public interface INavigation
{
    /// <summary>
    /// Returns the full navigation tree (topbar modules + sidebar items) for the given role.
    /// </summary>
    Task<Result<NavigationResponse, NavigationError>> GetNavigationAsync(string roleName);
}