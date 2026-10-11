using Application.Core.DTOs.Report.Errors;
using Application.Core.DTOs.Report.Request;
using Application.Core.DTOs.Report.Response;
using Application.Core.Interfaces.Report;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Report;

namespace Application.Core.Services.Report;

public sealed class ReportService : IReport
{
    private const int MaxRangeDays = 366;

    private readonly IReportRepository _repository;
    private readonly IActivityLogger _activityLogger;
    private readonly IUserContext _userContext;

    public ReportService(IReportRepository repository, IActivityLogger activityLogger, IUserContext userContext)
    {
        _repository = repository;
        _activityLogger = activityLogger;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<ActivityByUserItemResponse>, ReportError>> GetActivityByUserAsync(
        ReportDateRangeRequest request) =>
        ValidateRange(request)
            .BindAsync(range => _repository.GetActivityByUserAsync(range.From, range.ToExclusive)
                .MapErrorAsync(ReportError (error) =>
                    new ReportDataAccessError(error.Message, error.Details, error.Exception))
                .MapAsync(rows => (IReadOnlyList<ActivityByUserItemResponse>)rows
                    .Select(r => new ActivityByUserItemResponse
                    {
                        UserCode = r.UserCode,
                        Username = r.Username,
                        EventType = r.EventType,
                        EventCount = r.EventCount,
                        LastOccurredAt = r.LastOccurredAt
                    }).ToList()))
            .TapAsync(_ => LogViewedAsync("ActivityByUser", request));

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<UnattributedWriteItemResponse>, ReportError>> GetUnattributedWritesAsync(
        ReportDateRangeRequest request) =>
        ValidateRange(request)
            .BindAsync(range => _repository.GetUnattributedWritesAsync(range.From, range.ToExclusive)
                .MapErrorAsync(ReportError (error) =>
                    new ReportDataAccessError(error.Message, error.Details, error.Exception))
                .MapAsync(rows => (IReadOnlyList<UnattributedWriteItemResponse>)rows
                    .Select(r => new UnattributedWriteItemResponse
                    {
                        TableName = r.TableName,
                        Action = r.Action,
                        WriteCount = r.WriteCount,
                        LastOccurredAt = r.LastOccurredAt
                    }).ToList()))
            .TapAsync(_ => LogViewedAsync("UnattributedWrites", request));

    /// <summary>Converts the inclusive UTC day range into <c>[From, ToExclusive)</c>, rejecting inverted or oversized ranges.</summary>
    private static Task<Result<(DateTimeOffset From, DateTimeOffset ToExclusive), ReportError>> ValidateRange(
        ReportDateRangeRequest request)
    {
        bool valid = request.From <= request.To && request.To.DayNumber - request.From.DayNumber < MaxRangeDays;

        Result<(DateTimeOffset, DateTimeOffset), ReportError> result = valid
            ? Result<(DateTimeOffset, DateTimeOffset), ReportError>.Success((
                new DateTimeOffset(request.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                new DateTimeOffset(request.To.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)))
            : Result<(DateTimeOffset, DateTimeOffset), ReportError>.Failure(new ReportInvalidRangeError());

        return Task.FromResult(result);
    }

    /// <summary>Viewing audit reports is itself audited (architecture doc §12.5).</summary>
    private Task LogViewedAsync(string report, ReportDateRangeRequest request) =>
        _activityLogger.LogAsync(new ActivityEvent(
            ActivityEventTypes.ReportViewed, _userContext.UserCode, null, null, $"Viewed report {report}",
            new { Report = report, request.From, request.To }));
}
