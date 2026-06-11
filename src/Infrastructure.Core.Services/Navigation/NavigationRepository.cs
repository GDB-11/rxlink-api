using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Navigation;
using Infrastructure.Core.Interfaces.Navigation;
using Infrastructure.Core.Models.Navigation;

namespace Infrastructure.Core.Services.Navigation;

/// <summary>
/// Handles all database operations related to role-based navigation.
/// </summary>
public sealed class NavigationRepository : BaseDatabaseService, INavigationRepository
{
    private readonly IDbConnection _connection;

    public NavigationRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<NavigationRow>, NavigationRepositoryError>>
        GetRowsByRoleAsync(string roleName) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, NavigationRow>(
                _connection,
                NavigationRepositorySql.GetRowsByRole,
                new { RoleName = roleName }),
            errorFactory: NavigationRepositoryError (ex) => new GetNavigationRowsError(ex.Message, ex)
        );
}