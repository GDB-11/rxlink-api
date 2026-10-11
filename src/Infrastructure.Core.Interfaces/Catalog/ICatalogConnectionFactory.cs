using System.Data;

namespace Infrastructure.Core.Interfaces.Catalog;

/// <summary>
/// Opens connections to the shared catalog database. Deliberately not an <see cref="IDbConnection"/> registration:
/// it keeps catalog repositories from picking up the partner connection (and vice versa).
/// </summary>
public interface ICatalogConnectionFactory
{
    /// <summary>Creates a new, unopened connection. The caller disposes it.</summary>
    IDbConnection Create();
}
