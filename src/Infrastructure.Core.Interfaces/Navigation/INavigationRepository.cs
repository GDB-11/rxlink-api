using BindSharp;
using Infrastructure.Core.DTOs.Navigation;
using Infrastructure.Core.Models.Navigation;

namespace Infrastructure.Core.Interfaces.Navigation;

public interface INavigationRepository
{
    /// <summary>
    /// Returns the flat navigation rows for the given role name.
    /// Each row represents a (module, item) pair; grouping into a tree is the caller's responsibility.
    /// </summary>
    Task<Result<IEnumerable<NavigationRow>, NavigationRepositoryError>> GetRowsByRoleAsync(string roleName);
}