using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Infrastructure.Core.Services.Catalog;

public sealed class CatalogVersionReader : BaseDatabaseService, ICatalogVersionReader
{
    private readonly ICatalogConnectionFactory _connectionFactory;

    public CatalogVersionReader(ICatalogConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<CatalogVersionRow, CatalogRepositoryError>> GetAsync() =>
        await Result.TryAsync(
            operation: async () =>
            {
                using System.Data.IDbConnection connection = _connectionFactory.Create();
                return await ExecuteSingleOrDefaultAsync<object, CatalogVersionRow>(
                           connection, CatalogReaderSql.Version, new { })
                       ?? throw new InvalidOperationException("CatalogVersion has no row.");
            },
            errorFactory: CatalogRepositoryError (ex) => new CatalogLoadError(ex.Message, ex)
        );
}
