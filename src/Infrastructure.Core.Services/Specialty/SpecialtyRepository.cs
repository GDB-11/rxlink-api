using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Specialty;
using Infrastructure.Core.Interfaces.Specialty;
using Infrastructure.Core.Models.Specialty;

namespace Infrastructure.Core.Services.Specialty;

public sealed class SpecialtyRepository : BaseDatabaseService, ISpecialtyRepository
{
    private readonly IDbConnection _connection;

    public SpecialtyRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<SpecialtyRow>, SpecialtyRepositoryError>> GetPageAsync(
        int offset, int limit, string? search) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, SpecialtyRow>(
                _connection,
                SpecialtyRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search }),
            errorFactory: SpecialtyRepositoryError (ex) => new GetSpecialtyPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<SpecialtyRow?, SpecialtyRepositoryError>> InsertAsync(
        string name) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, SpecialtyRow>(
                _connection,
                SpecialtyRepositorySql.Insert,
                new
                {
                    Name = name,
                }),
            errorFactory: SpecialtyRepositoryError (ex) => new InsertSpecialtyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<SpecialtyRow?, SpecialtyRepositoryError>> UpdateAsync(
        Guid code, string name) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, SpecialtyRow>(
                _connection,
                SpecialtyRepositorySql.Update,
                new
                {
                    Code = code,
                    Name = name,
                }),
            errorFactory: SpecialtyRepositoryError (ex) => new UpdateSpecialtyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, SpecialtyRepositoryError>> DeactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                SpecialtyRepositorySql.Deactivate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: SpecialtyRepositoryError (ex) => new DeactivateSpecialtyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, SpecialtyRepositoryError>> ActivateAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                SpecialtyRepositorySql.Activate,
                new { Code = code }),
            errorFactory: SpecialtyRepositoryError (ex) => new DeactivateSpecialtyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<SpecialtyWithDoctorCountRow>, SpecialtyRepositoryError>>
        GetAllActiveWithDoctorCountAsync() =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, SpecialtyWithDoctorCountRow>(
                _connection,
                SpecialtyRepositorySql.GetAllActiveWithDoctorCount,
                new { }),
            errorFactory: SpecialtyRepositoryError (ex) => new GetActiveSpecialtiesWithCountError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<DoctorSummaryRow>?, SpecialtyRepositoryError>> GetDoctorsBySpecialtyCodeAsync(
        Guid specialtyCode) =>
        await Result.TryAsync(
            operation: async () =>
            {
                IEnumerable<DoctorSummaryRow> rows = await ExecuteQueryAsync<object, DoctorSummaryRow>(
                    _connection,
                    SpecialtyRepositorySql.GetDoctorsBySpecialtyCode,
                    new { SpecialtyCode = specialtyCode });

                List<DoctorSummaryRow> list = rows.ToList();

                // 0 rows = specialty not found or inactive
                if (list.Count == 0)
                    return null;

                // Filter out sentinel row (specialty exists but no doctors yet)
                return (IEnumerable<DoctorSummaryRow>)list.Where(r => r.UserCode.HasValue).ToList();
            },
            errorFactory: SpecialtyRepositoryError (ex) => new GetDoctorsBySpecialtyError(ex.Message, ex)
        );
}