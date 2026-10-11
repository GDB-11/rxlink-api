using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Infrastructure.Core.Services.Catalog;

public sealed class PartnerRegistryReader : BaseDatabaseService, IPartnerRegistryReader
{
    private readonly ICatalogConnectionFactory _connectionFactory;

    public PartnerRegistryReader(ICatalogConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<PartnerIdentityRow?, CatalogRepositoryError>> GetIdentityAsync() =>
        await Result.TryAsync(
            operation: async () =>
            {
                using System.Data.IDbConnection connection = _connectionFactory.Create();
                return await ExecuteSingleOrDefaultAsync<object, PartnerIdentityRow>(
                    connection, PartnerRegistryReaderSql.Identity, new { });
            },
            errorFactory: CatalogRepositoryError (ex) => new CatalogLoadError(ex.Message, ex)
        );

    public async Task<Result<PartnerLogoRow?, CatalogRepositoryError>> GetLogoAsync() =>
        await Result.TryAsync(
            operation: async () =>
            {
                using System.Data.IDbConnection connection = _connectionFactory.Create();
                return await ExecuteSingleOrDefaultAsync<object, PartnerLogoRow>(
                    connection, PartnerRegistryReaderSql.Logo, new { });
            },
            errorFactory: CatalogRepositoryError (ex) => new CatalogLoadError(ex.Message, ex)
        );
}
