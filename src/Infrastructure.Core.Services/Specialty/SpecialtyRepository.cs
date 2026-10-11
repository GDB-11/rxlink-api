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
        Guid specialtyCode, string? search = null) =>
        await Result.TryAsync(
            operation: async () =>
            {
                IEnumerable<DoctorSummaryRow> rows = await ExecuteQueryAsync<object, DoctorSummaryRow>(
                    _connection,
                    SpecialtyRepositorySql.GetDoctorsBySpecialtyCode,
                    new { SpecialtyCode = specialtyCode, Search = search });

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