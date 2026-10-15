using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PrivacyNotice.Errors;
using Application.Core.DTOs.PrivacyNotice.Request;
using Application.Core.DTOs.PrivacyNotice.Response;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Interfaces.PrivacyNotice;
using Application.Core.Services.PatientAccess;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.DTOs.PrivacyNotice;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.PrivacyNotice;
using Infrastructure.Core.Models.PrivacyNotice;

namespace Application.Core.Services.PrivacyNotice;

public sealed class PrivacyNoticeService : IPrivacyNotice
{
    private const int MaxPageSize = 100;
    private const int MinBodyLength = 20;

    private readonly IPrivacyNoticeRepository _repository;
    private readonly IPatientAccessPolicy _accessPolicy;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IActivityLogger _activityLogger;

    public PrivacyNoticeService(IPrivacyNoticeRepository repository, IPatientAccessPolicy accessPolicy,
        IRecordAccessLogger accessLogger, IActivityLogger activityLogger)
    {
        _repository = repository;
        _accessPolicy = accessPolicy;
        _accessLogger = accessLogger;
        _activityLogger = activityLogger;
    }

    /// <inheritdoc/>
    public Task<Result<PrivacyNoticeResponse, PrivacyNoticeError>> PublishAsync(
        PublishPrivacyNoticeRequest request, Guid publishedByUserCode)
    {
        string title = request.Title.Trim();
        string body = request.Body.Trim();

        if (title.Length == 0)
            return Fail<PrivacyNoticeResponse>(new PrivacyNoticeInvalidError("El título es obligatorio."));
        if (body.Length < MinBodyLength)
            return Fail<PrivacyNoticeResponse>(
                new PrivacyNoticeInvalidError($"El texto del aviso debe tener al menos {MinBodyLength} caracteres."));
        if (request.EffectiveFrom < TodayInLima())
            return Fail<PrivacyNoticeResponse>(
                new PrivacyNoticeInvalidError("La fecha de vigencia no puede ser anterior a hoy."));

        return _repository.PublishAsync(title, body, request.EffectiveFrom, publishedByUserCode)
            .MapErrorAsync(ToDataError)
            .EnsureNotNullAsync(new PrivacyNoticeDataAccessError("No se pudo publicar el aviso de privacidad."))
            // Version only: the event never carries the notice text.
            .TapAsync(row => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.PrivacyNoticePublished, publishedByUserCode, ActivityEntityTypes.PrivacyNotice,
                row.PrivacyNoticeCode, "Privacy notice published", new { row.Version })))
            .MapAsync(MapToResponse);
    }

    /// <inheritdoc/>
    public Task<Result<PrivacyNoticePageResponse, PrivacyNoticeError>> GetPageAsync(PrivacyNoticePageRequest request)
    {
        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        return _repository.GetPageAsync((page - 1) * pageSize, pageSize)
            .MapErrorAsync(ToDataError)
            .MapAsync(rows =>
            {
                List<PrivacyNoticeRow> list = rows.ToList();
                int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
                return new PrivacyNoticePageResponse
                {
                    Items = list.Select(MapToResponse).ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            });
    }

    /// <inheritdoc/>
    public Task<Result<PrivacyNoticeResponse, PrivacyNoticeError>> GetCurrentAsync() =>
        _repository.GetCurrentAsync()
            .MapErrorAsync(ToDataError)
            .EnsureNotNullAsync(new PrivacyNoticeNotPublishedError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> GetStatusAsync(Guid patientCode) =>
        RequireStaffAccessAsync(patientCode)
            .BindAsync(decision => ReadStatusAsync(patientCode)
                .TapAsync(_ => _accessLogger.LogAsync(AccessResourceTypes.PrivacyNotice, patientCode,
                    viaBreakGlass: decision == PatientAccessDecision.AllowedViaBreakGlass)));

    /// <inheritdoc/>
    public Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> AcknowledgeAsync(
        Guid patientCode, Guid recordedByUserCode) =>
        RequireStaffAccessAsync(patientCode)
            .BindAsync(_ => AcknowledgeCurrentAsync(patientCode, recordedByUserCode));

    /// <inheritdoc/>
    public Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> GetOwnStatusAsync(Guid patientCode) =>
        ReadStatusAsync(patientCode);

    /// <inheritdoc/>
    public Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> AcknowledgeOwnAsync(Guid patientCode) =>
        AcknowledgeCurrentAsync(patientCode, actorUserCode: null);

    private Task<Result<PatientAccessDecision, PrivacyNoticeError>> RequireStaffAccessAsync(Guid patientCode) =>
        _accessPolicy.RequireAsync<PrivacyNoticeError>(patientCode, PatientDataScope.Administrative,
            error => new PrivacyNoticeDataAccessError(error.Message, error.Details, error.Exception),
            new PrivacyNoticeAccessDeniedError());

    private async Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> AcknowledgeCurrentAsync(
        Guid patientCode, Guid? actorUserCode)
    {
        Result<int, PrivacyNoticeRepositoryError> written =
            await _repository.AcknowledgeCurrentAsync(patientCode, actorUserCode);
        if (!written.IsSuccess)
            return Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>.Failure(ToDataError(written.Error!));

        Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError> status = await ReadStatusAsync(patientCode);
        if (!status.IsSuccess)
            return status;

        PatientPrivacyNoticeStatusResponse value = status.Value!;
        if (value.Current is null)
            return Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>.Failure(
                new PrivacyNoticeNotPublishedError());

        // Nothing written and not acknowledged either: the patient does not exist (an existing acknowledgment is
        // an idempotent success).
        if (written.Value == 0 && !value.CurrentAcknowledged)
            return Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>.Failure(
                new PrivacyNoticePatientNotFoundError());

        if (written.Value > 0)
            await _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.PrivacyNoticeAcknowledged, actorUserCode, ActivityEntityTypes.PrivacyNotice,
                value.Current.PrivacyNoticeCode, "Privacy notice acknowledged",
                new { PatientCode = patientCode, value.Current.Version, ByPatient = actorUserCode is null }));

        return status;
    }

    private async Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> ReadStatusAsync(
        Guid patientCode)
    {
        Result<PrivacyNoticeRow?, PrivacyNoticeRepositoryError> current = await _repository.GetCurrentAsync();
        if (!current.IsSuccess)
            return Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>.Failure(ToDataError(current.Error!));

        Result<IEnumerable<PrivacyNoticeAcknowledgmentRow>, PrivacyNoticeRepositoryError> acks =
            await _repository.GetAcknowledgmentsAsync(patientCode);
        if (!acks.IsSuccess)
            return Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>.Failure(ToDataError(acks.Error!));

        List<PrivacyNoticeAcknowledgmentResponse> items = acks.Value!.Select(a => new PrivacyNoticeAcknowledgmentResponse
        {
            PrivacyNoticeCode = a.PrivacyNoticeCode,
            Version = a.Version,
            Title = a.Title,
            AcknowledgedAt = a.AcknowledgedAt,
            RecordedByName = a.RecordedByName
        }).ToList();

        PrivacyNoticeRow? notice = current.Value;
        return Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>.Success(
            new PatientPrivacyNoticeStatusResponse
            {
                Current = notice is null ? null : MapToResponse(notice),
                CurrentAcknowledged = notice is not null
                                      && items.Any(a => a.PrivacyNoticeCode == notice.PrivacyNoticeCode),
                Acknowledgments = items
            });
    }

    private static DateOnly TodayInLima() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/Lima")).DateTime);

    private static Task<Result<T, PrivacyNoticeError>> Fail<T>(PrivacyNoticeError error) =>
        Task.FromResult(Result<T, PrivacyNoticeError>.Failure(error));

    private static PrivacyNoticeError ToDataError(PrivacyNoticeRepositoryError error) =>
        new PrivacyNoticeDataAccessError(error.Message, error.Details, error.Exception);

    private static PrivacyNoticeResponse MapToResponse(PrivacyNoticeRow row) =>
        new()
        {
            PrivacyNoticeCode = row.PrivacyNoticeCode,
            Version = row.Version,
            Title = row.Title,
            Body = row.Body,
            EffectiveFrom = row.EffectiveFrom,
            PublishedAt = row.PublishedAt
        };
}
