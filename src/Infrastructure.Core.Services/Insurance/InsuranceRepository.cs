using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Insurance;
using Infrastructure.Core.Interfaces.Insurance;
using Infrastructure.Core.Models.Insurance;

namespace Infrastructure.Core.Services.Insurance;

public sealed class InsuranceRepository : BaseDatabaseService, IInsuranceRepository
{
    private readonly IDbConnection _connection;

    public InsuranceRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<InsuranceRow>, InsuranceRepositoryError>> GetPageAsync(
        int offset, int limit, string? search) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, InsuranceRow>(
                _connection,
                InsuranceRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search }),
            errorFactory: InsuranceRepositoryError (ex) => new GetInsurancePageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<InsuranceRow?, InsuranceRepositoryError>> InsertAsync(
        string name, decimal coveragePercentage) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, InsuranceRow>(
                _connection,
                InsuranceRepositorySql.Insert,
                new
                {
                    Name = name,
                    CoveragePercentage = coveragePercentage,
                }),
            errorFactory: InsuranceRepositoryError (ex) => new InsertInsuranceError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<InsuranceRow?, InsuranceRepositoryError>> UpdateAsync(
        Guid code, string name, decimal coveragePercentage) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, InsuranceRow>(
                _connection,
                InsuranceRepositorySql.Update,
                new
                {
                    Code = code,
                    Name = name,
                    CoveragePercentage = coveragePercentage,
                }),
            errorFactory: InsuranceRepositoryError (ex) => new UpdateInsuranceError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, InsuranceRepositoryError>> DeactivateAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                InsuranceRepositorySql.Deactivate,
                new { Code = code }),
            errorFactory: InsuranceRepositoryError (ex) => new DeactivateInsuranceError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, InsuranceRepositoryError>> ActivateAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                InsuranceRepositorySql.Activate,
                new { Code = code }),
            errorFactory: InsuranceRepositoryError (ex) => new DeactivateInsuranceError(ex.Message, ex)
        );
}
