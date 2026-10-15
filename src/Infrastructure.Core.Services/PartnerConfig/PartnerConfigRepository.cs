using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerConfig;
using Infrastructure.Core.Interfaces.PartnerConfig;

namespace Infrastructure.Core.Services.PartnerConfig;

public sealed class PartnerConfigRepository : BaseDatabaseService, IPartnerConfigRepository
{
    private const string GetPartnerCode = """SELECT "PartnerCode" FROM "PartnerConfig" WHERE "PartnerConfigId" = 1""";

    private readonly IDbConnection _connection;

    public PartnerConfigRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid?, PartnerConfigRepositoryError>> GetPartnerCodeAsync() =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, Guid?>(_connection, GetPartnerCode, new { }),
            errorFactory: PartnerConfigRepositoryError (ex) => new GetPartnerCodeError(ex.Message, ex)
        );
}
