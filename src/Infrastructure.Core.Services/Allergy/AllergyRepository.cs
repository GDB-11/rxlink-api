using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Allergy;
using Infrastructure.Core.Interfaces.Allergy;
using Infrastructure.Core.Models.Allergy;

namespace Infrastructure.Core.Services.Allergy;

/// <summary>
/// Handles all database operations for the Allergy catalog.
/// </summary>
public sealed class AllergyRepository : BaseDatabaseService, IAllergyRepository
{
    private readonly IDbConnection _connection;

    public AllergyRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AllergyRow>, AllergyRepositoryError>> GetPageAsync(
        int offset, int limit, string? search) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, AllergyRow>(
                _connection,
                AllergyRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search }),
            errorFactory: AllergyRepositoryError (ex) => new GetAllergiesPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<AllergyRow?, AllergyRepositoryError>> InsertAsync(
        string name, string? description) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, AllergyRow>(
                _connection,
                AllergyRepositorySql.Insert,
                new { Name = name, Description = description }),
            errorFactory: AllergyRepositoryError (ex) => new InsertAllergyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<AllergyRow?, AllergyRepositoryError>> UpdateAsync(
        Guid code, string name, string? description) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, AllergyRow>(
                _connection,
                AllergyRepositorySql.Update,
                new { Code = code, Name = name, Description = description }),
            errorFactory: AllergyRepositoryError (ex) => new UpdateAllergyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, AllergyRepositoryError>> DeactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                AllergyRepositorySql.Deactivate,
                new { Code = code }),
            errorFactory: AllergyRepositoryError (ex) => new DeactivateAllergyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, AllergyRepositoryError>> ActivateAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                AllergyRepositorySql.Activate,
                new { Code = code }),
            errorFactory: AllergyRepositoryError (ex) => new DeactivateAllergyError(ex.Message, ex)
        );
}