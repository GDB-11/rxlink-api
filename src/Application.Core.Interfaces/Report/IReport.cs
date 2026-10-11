using Application.Core.DTOs.Report.Errors;
using Application.Core.DTOs.Report.Request;
using Application.Core.DTOs.Report.Response;
using BindSharp;

namespace Application.Core.Interfaces.Report;

public interface IReport
{
    /// <summary>Event counts per user and event type, including <c>Record.Accessed</c> (the access log).</summary>
    Task<Result<IReadOnlyList<ActivityByUserItemResponse>, ReportError>> GetActivityByUserAsync(
        ReportDateRangeRequest request);

    /// <summary>Audited writes with no actor: a signal that some write path missed the audit context.</summary>
    Task<Result<IReadOnlyList<UnattributedWriteItemResponse>, ReportError>> GetUnattributedWritesAsync(
        ReportDateRangeRequest request);
}
