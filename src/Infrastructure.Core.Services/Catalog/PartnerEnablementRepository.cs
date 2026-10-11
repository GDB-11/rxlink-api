using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Interfaces.Catalog;

namespace Infrastructure.Core.Services.Catalog;

public sealed class PartnerEnablementRepository : BaseDatabaseService, IPartnerEnablementRepository
{
    private readonly IDbConnection _connection;

    public PartnerEnablementRepository(IDbConnection connection) => _connection = connection;

    /// <inheritdoc/>
    public async Task<Result<IReadOnlySet<Guid>, CatalogRepositoryError>> GetEnabledAsync(
        PartnerEnablementKind kind, IReadOnlyCollection<Guid> codes)
    {
        if (codes.Count == 0)
            return Result<IReadOnlySet<Guid>, CatalogRepositoryError>.Success(new HashSet<Guid>());

        string sql = kind switch
        {
            PartnerEnablementKind.Medication => PartnerEnablementRepositorySql.EnabledMedications,
            PartnerEnablementKind.Specialty => PartnerEnablementRepositorySql.EnabledSpecialties,
            PartnerEnablementKind.Insurance => PartnerEnablementRepositorySql.EnabledInsurances,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        return await Result.TryAsync(
            operation: async () =>
            {
                IEnumerable<Guid> rows = await ExecuteQueryAsync<object, Guid>(_connection, sql,
                    new { Codes = codes.ToArray() });
                return (IReadOnlySet<Guid>)rows.ToHashSet();
            },
            errorFactory: CatalogRepositoryError (ex) => new PartnerEnablementReadError(ex.GetType().Name, ex));
    }
}
