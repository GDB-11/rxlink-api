using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.DTOs.PatientAccess.Request;
using Application.Core.DTOs.PatientAccess.Response;
using Application.Core.DTOs.Report.Request;
using Application.Core.Interfaces.PatientAccess;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.PatientAccess;

namespace Application.Core.Services.PatientAccess;

public sealed class BreakGlassService : IBreakGlass
{
    /// <summary>How long a grant keeps a patient open to the nurse.</summary>
    internal const int GrantMinutes = 4 * 60;

    private const int MaxRangeDays = 366;

    private readonly IPatientAccessRepository _repository;
    private readonly IActivityLogger _activityLogger;
    private readonly IUserContext _userContext;

    public BreakGlassService(IPatientAccessRepository repository, IActivityLogger activityLogger,
        IUserContext userContext)
    {
        _repository = repository;
        _activityLogger = activityLogger;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public Task<Result<BreakGlassResponse, PatientAccessError>> RequestAsync(BreakGlassRequest request)
    {
        if (_userContext.Role != "Enfermero" || _userContext.UserCode is not { } userCode)
            return Task.FromResult(
                Result<BreakGlassResponse, PatientAccessError>.Failure(new PatientAccessNotPermittedError()));

        return _repository.GrantBreakGlassAsync(
                userCode, request.MedicalRecordNumber.Trim(), request.Reason.Trim(), GrantMinutes)
            .MapErrorAsync(PatientAccessError (error) =>
                new PatientAccessDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new BreakGlassPatientNotFoundError())
            .MapAsync(row => new BreakGlassResponse { PatientCode = row.PatientCode, ExpiresAt = row.ExpiresAt });
    }

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<BreakGlassReportItemResponse>, PatientAccessError>> GetReportAsync(
        ReportDateRangeRequest request)
    {
        bool valid = request.From <= request.To && request.To.DayNumber - request.From.DayNumber < MaxRangeDays;
        if (!valid)
            return Task.FromResult(Result<IReadOnlyList<BreakGlassReportItemResponse>, PatientAccessError>
                .Failure(new BreakGlassReportInvalidRangeError()));

        DateTimeOffset from = new(request.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        DateTimeOffset toExclusive = new(request.To.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return _repository.GetBreakGlassReportAsync(from, toExclusive)
            .MapErrorAsync(PatientAccessError (error) =>
                new PatientAccessDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => (IReadOnlyList<BreakGlassReportItemResponse>)rows
                .Select(r => new BreakGlassReportItemResponse
                {
                    GrantCode = r.GrantCode,
                    UserCode = r.UserCode,
                    Username = r.Username,
                    PatientCode = r.PatientCode,
                    MedicalRecordNumber = r.MedicalRecordNumber,
                    Reason = r.Reason,
                    GrantedAt = r.GrantedAt,
                    ExpiresAt = r.ExpiresAt
                }).ToList())
            // Viewing audit reports is itself audited (architecture doc §12.5).
            .TapAsync(_ => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.ReportViewed, _userContext.UserCode, null, null, "Viewed report BreakGlass",
                new { Report = "BreakGlass", request.From, request.To })));
    }
}
