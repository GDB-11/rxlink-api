using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Interfaces.Catalog;

namespace Infrastructure.Core.Services.Catalog;

/// <summary>Reads all rows of one catalog table using the catalog connection (never the partner one).</summary>
public sealed class CatalogReader<TRow> : BaseDatabaseService, ICatalogReader<TRow>
{
    private readonly ICatalogConnectionFactory _connectionFactory;
    private readonly string _sql;

    public CatalogReader(ICatalogConnectionFactory connectionFactory, string sql)
    {
        _connectionFactory = connectionFactory;
        _sql = sql;
    }

    public async Task<Result<IReadOnlyList<TRow>, CatalogRepositoryError>> GetAllAsync() =>
        await Result.TryAsync(
            operation: async () =>
            {
                using System.Data.IDbConnection connection = _connectionFactory.Create();
                return (IReadOnlyList<TRow>)(await ExecuteQueryAsync<TRow>(connection, _sql)).ToList();
            },
            errorFactory: CatalogRepositoryError (ex) => new CatalogLoadError(ex.Message, ex)
        );
}
