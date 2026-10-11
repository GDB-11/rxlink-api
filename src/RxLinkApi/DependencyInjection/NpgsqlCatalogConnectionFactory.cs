using System.Data;
using Infrastructure.Core.Interfaces.Catalog;
using Npgsql;

namespace RxLinkApi.DependencyInjection;

/// <summary>
/// Opens connections with the read-only catalog credential. The connection string is validated on first use, not at
/// startup, so a missing setting degrades the catalog instead of stopping the API from booting.
/// </summary>
internal sealed class NpgsqlCatalogConnectionFactory : ICatalogConnectionFactory
{
    // Every partner opens connections to the same catalog server, so keep each partner's pool small (plan §7.1).
    private const int DefaultMaxPoolSize = 5;

    private readonly string? _connectionString;

    public NpgsqlCatalogConnectionFactory(string? connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection Create()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("ConnectionStrings:CatalogConnection is not configured.");

        NpgsqlConnectionStringBuilder builder = new(_connectionString);
        if (!builder.ContainsKey("Maximum Pool Size"))
            builder.MaxPoolSize = DefaultMaxPoolSize;

        return new NpgsqlConnection(builder.ConnectionString);
    }
}
