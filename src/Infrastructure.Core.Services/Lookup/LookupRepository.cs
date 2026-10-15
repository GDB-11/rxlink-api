using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Lookup;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Models.Lookup;

namespace Infrastructure.Core.Services.Lookup;

public sealed class LookupRepository : BaseDatabaseService, ILookupRepository
{
    private readonly IDbConnection _connection;

    public LookupRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveRolesAsync() =>
        await Result.TryAsync(
            operation: async () =>
                await ExecuteQueryAsync<GuidLookupRow>(_connection, LookupRepositorySql.GetActiveRoles),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActivePrescriptionStatusesAsync() =>
        await Result.TryAsync(
            operation: async () =>
                await ExecuteQueryAsync<GuidLookupRow>(_connection, LookupRepositorySql.GetActivePrescriptionStatuses),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveConsultationTypesAsync() =>
        await Result.TryAsync(
            operation: async () =>
                await ExecuteQueryAsync<GuidLookupRow>(_connection, LookupRepositorySql.GetActiveConsultationTypes),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );
}
