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
    public async Task<Result<IEnumerable<LookupRow>, LookupRepositoryError>> GetPharmaceuticalFormsAsync() =>
        await Result.TryAsync(
            operation:    async () => await ExecuteQueryAsync<LookupRow>(_connection, LookupRepositorySql.GetPharmaceuticalForms),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<LookupRow>, LookupRepositoryError>> GetAdministrationRoutesAsync() =>
        await Result.TryAsync(
            operation:    async () => await ExecuteQueryAsync<LookupRow>(_connection, LookupRepositorySql.GetAdministrationRoutes),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetSexesAsync() =>
        await Result.TryAsync(
            operation:    async () => await ExecuteQueryAsync<GuidLookupRow>(_connection, LookupRepositorySql.GetSexes),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveDocumentTypesAsync() =>
        await Result.TryAsync(
            operation:    async () => await ExecuteQueryAsync<GuidLookupRow>(_connection, LookupRepositorySql.GetActiveDocumentTypes),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveRolesAsync() =>
        await Result.TryAsync(
            operation:    async () => await ExecuteQueryAsync<GuidLookupRow>(_connection, LookupRepositorySql.GetActiveRoles),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveSpecialtiesAsync() =>
        await Result.TryAsync(
            operation:    async () => await ExecuteQueryAsync<GuidLookupRow>(_connection, LookupRepositorySql.GetActiveSpecialties),
            errorFactory: LookupRepositoryError (ex) => new GetLookupError(ex.Message, ex)
        );
}
