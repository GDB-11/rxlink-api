using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Report;
using Infrastructure.Core.Interfaces.Report;
using Infrastructure.Core.Models.Report;

namespace Infrastructure.Core.Services.Report;

public sealed class ReportRepository : BaseDatabaseService, IReportRepository
{
    private readonly IDbConnection _connection;

    public ReportRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ActivityByUserRow>, ReportRepositoryError>> GetActivityByUserAsync(
        DateTimeOffset from, DateTimeOffset toExclusive) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, ActivityByUserRow>(
                _connection,
                ReportRepositorySql.GetActivityByUser,
                new { From = from, ToExclusive = toExclusive }),
            errorFactory: ReportRepositoryError (ex) => new GetActivityByUserError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<UnattributedWriteRow>, ReportRepositoryError>> GetUnattributedWritesAsync(
        DateTimeOffset from, DateTimeOffset toExclusive) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, UnattributedWriteRow>(
                _connection,
                ReportRepositorySql.GetUnattributedWrites,
                new { From = from, ToExclusive = toExclusive }),
            errorFactory: ReportRepositoryError (ex) => new GetUnattributedWritesError(ex.Message, ex)
        );
}
