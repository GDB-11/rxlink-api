using Application.Core.DTOs.Report.Errors;
using Application.Core.DTOs.Report.Request;
using Application.Core.Services.Report;
using BindSharp;
using Infrastructure.Core.DTOs.Report;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Report;
using Infrastructure.Core.Models.Report;
using NSubstitute;

namespace Application.Core.Services.Tests.Report;

public sealed class ReportServiceTests
{
    private readonly IReportRepository _repository = Substitute.For<IReportRepository>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly ReportService _sut;

    public ReportServiceTests() => _sut = new ReportService(_repository, _activityLogger, _userContext);

    [Fact]
    public async Task GetActivityByUserAsync_InvertedRange_ReturnsInvalidRangeError()
    {
        var result = await _sut.GetActivityByUserAsync(new ReportDateRangeRequest
            { From = new DateOnly(2026, 5, 2), To = new DateOnly(2026, 5, 1) });

        Assert.True(result.IsFailure);
        Assert.IsType<ReportInvalidRangeError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().GetActivityByUserAsync(default, default);
    }

    [Fact]
    public async Task GetActivityByUserAsync_RangeOverOneYear_ReturnsInvalidRangeError()
    {
        var result = await _sut.GetActivityByUserAsync(new ReportDateRangeRequest
            { From = new DateOnly(2025, 1, 1), To = new DateOnly(2026, 1, 2) });

        Assert.True(result.IsFailure);
        Assert.IsType<ReportInvalidRangeError>(result.Error);
    }

    [Fact]
    public async Task GetActivityByUserAsync_ValidRange_UsesInclusiveDayBoundsAndAuditsTheView()
    {
        _repository.GetActivityByUserAsync(default, default).ReturnsForAnyArgs(Task.FromResult(
            Result<IEnumerable<ActivityByUserRow>, ReportRepositoryError>.Success([
                new ActivityByUserRow
                {
                    UserCode = Guid.NewGuid(), Username = "dr", EventType = "Record.Accessed",
                    EventCount = 3, LastOccurredAt = DateTimeOffset.UtcNow
                }
            ])));

        var result = await _sut.GetActivityByUserAsync(new ReportDateRangeRequest
            { From = new DateOnly(2026, 5, 1), To = new DateOnly(2026, 5, 1) });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        await _repository.Received(1).GetActivityByUserAsync(
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero));
        await _activityLogger.Received(1).LogAsync(
            Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.ReportViewed));
    }

    [Fact]
    public async Task GetUnattributedWritesAsync_RepositoryFails_ReturnsDataAccessError()
    {
        _repository.GetUnattributedWritesAsync(default, default).ReturnsForAnyArgs(Task.FromResult(
            Result<IEnumerable<UnattributedWriteRow>, ReportRepositoryError>.Failure(
                new GetUnattributedWritesError("boom"))));

        var result = await _sut.GetUnattributedWritesAsync(new ReportDateRangeRequest
            { From = new DateOnly(2026, 5, 1), To = new DateOnly(2026, 5, 2) });

        Assert.True(result.IsFailure);
        Assert.IsType<ReportDataAccessError>(result.Error);
        await _activityLogger.DidNotReceiveWithAnyArgs().LogAsync(default!);
    }
}
