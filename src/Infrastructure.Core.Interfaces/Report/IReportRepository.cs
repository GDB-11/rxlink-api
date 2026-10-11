using BindSharp;
using Infrastructure.Core.DTOs.Report;
using Infrastructure.Core.Models.Report;

namespace Infrastructure.Core.Interfaces.Report;

public interface IReportRepository
{
    /// <param name="from">Inclusive lower bound.</param>
    /// <param name="toExclusive">Exclusive upper bound.</param>
    Task<Result<IEnumerable<ActivityByUserRow>, ReportRepositoryError>> GetActivityByUserAsync(
        DateTimeOffset from, DateTimeOffset toExclusive);

    Task<Result<IEnumerable<UnattributedWriteRow>, ReportRepositoryError>> GetUnattributedWritesAsync(
        DateTimeOffset from, DateTimeOffset toExclusive);
}
