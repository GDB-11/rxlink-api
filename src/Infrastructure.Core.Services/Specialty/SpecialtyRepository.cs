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
    public async Task<Result<IReadOnlyList<SpecialtyDoctorCountRow>, SpecialtyRepositoryError>> GetDoctorCountsAsync() =>
        await Result.TryAsync(
            operation: async () => (IReadOnlyList<SpecialtyDoctorCountRow>)(await ExecuteQueryAsync<object, SpecialtyDoctorCountRow>(
                _connection,
                SpecialtyRepositorySql.GetDoctorCountsBySpecialtyCode,
                new { })).ToList(),
            errorFactory: SpecialtyRepositoryError (ex) => new GetActiveSpecialtiesWithCountError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<DoctorSummaryRow>, SpecialtyRepositoryError>> GetDoctorsBySpecialtyCodeAsync(
        Guid specialtyCode, string? search = null) =>
        await Result.TryAsync(
            operation: async () => (IReadOnlyList<DoctorSummaryRow>)(await ExecuteQueryAsync<object, DoctorSummaryRow>(
                _connection,
                SpecialtyRepositorySql.GetDoctorsBySpecialtyCode,
                new { SpecialtyCode = specialtyCode, Search = search })).ToList(),
            errorFactory: SpecialtyRepositoryError (ex) => new GetDoctorsBySpecialtyError(ex.Message, ex)
        );
}
