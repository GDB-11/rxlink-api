using Application.Core.DTOs.Navigation.Errors;
using Application.Core.DTOs.Navigation.Response;
using Application.Core.Interfaces.Navigation;
using BindSharp;
using Infrastructure.Core.Interfaces.Navigation;
using Infrastructure.Core.Models.Navigation;

namespace Application.Core.Services.Navigation;

public sealed class NavigationService : INavigation
{
    private readonly INavigationRepository _navigationRepository;

    public NavigationService(INavigationRepository navigationRepository)
    {
        _navigationRepository = navigationRepository;
    }

    /// <inheritdoc/>
    public Task<Result<NavigationResponse, NavigationError>> GetNavigationAsync(string roleName) =>
        _navigationRepository.GetRowsByRoleAsync(roleName)
            .MapErrorAsync(NavigationError (error) => new NavigationDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(BuildNavigationResponse);

    private static NavigationResponse BuildNavigationResponse(IEnumerable<NavigationRow> rows)
    {
        IReadOnlyList<NavigationModuleResponse> modules = rows
            .GroupBy(r => r.ModuleCode)
            .Select(g => new NavigationModuleResponse
            {
                ModuleCode = g.Key,
                Label = g.First().ModuleLabel,
                Icon = g.First().ModuleIcon,
                Order = g.First().ModuleOrder,
                Items = g
                    .Where(r => r.ItemCode.HasValue)
                    .Select(MapItem)
                    .OrderBy(i => i.Order)
                    .ToList()
            })
            .OrderBy(m => m.Order)
            .ToList();

        return new NavigationResponse { Modules = modules };
    }

    private static NavigationItemResponse MapItem(NavigationRow row) =>
        new()
        {
            ItemCode = row.ItemCode!.Value,
            Label = row.ItemLabel!,
            Icon = row.ItemIcon!,
            Path = row.ItemPath!,
            Order = row.ItemOrder!.Value
        };
}