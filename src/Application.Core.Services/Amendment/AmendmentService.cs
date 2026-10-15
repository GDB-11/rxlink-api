using Application.Core.DTOs.Amendment;
using Application.Core.DTOs.Amendment.Errors;
using Application.Core.DTOs.Amendment.Request;
using Application.Core.DTOs.Amendment.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.Interfaces.Amendment;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.PatientAccess;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Amendment;
using Infrastructure.Core.Models.Amendment;

namespace Application.Core.Services.Amendment;

public sealed class AmendmentService : IAmendment
{
    private const int MaxPageSize = 100;

    private readonly IAmendmentRepository _repository;
    private readonly IPatientAccessPolicy _accessPolicy;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IActivityLogger _activityLogger;

    public AmendmentService(IAmendmentRepository repository, IPatientAccessPolicy accessPolicy,
        IRecordAccessLogger accessLogger, IActivityLogger activityLogger)
    {
        _repository = repository;
        _accessPolicy = accessPolicy;
        _accessLogger = accessLogger;
        _activityLogger = activityLogger;
    }

    /// <inheritdoc/>
    public Task<Result<AmendmentResponse, AmendmentError>> CreateAsync(
        Guid patientCode, CreateAmendmentRequest request)
    {
        string area = request.TargetArea.Trim();
        string change = request.RequestedChange.Trim();

        if (!AmendmentTargetAreas.All.Contains(area))
            return Fail<AmendmentResponse>(new AmendmentInvalidError("El área indicada no es válida."));
        if (change.Length < 10)
            return Fail<AmendmentResponse>(
                new AmendmentInvalidError("Describa el cambio solicitado con al menos 10 caracteres."));

        return _repository.InsertAsync(patientCode, area, change)
            .MapErrorAsync(ToDataError)
            .EnsureNotNullAsync(new AmendmentPatientNotFoundError())
            // The patient is the actor; no staff UserCode, and no text of the request in the event.
            .TapAsync(row => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.AmendmentRequested, null, ActivityEntityTypes.AmendmentRequest,
                row.AmendmentRequestCode, "Amendment requested by patient", new { TargetArea = area })))
            .MapAsync(MapToResponse);
    }

    /// <inheritdoc/>
    public Task<Result<AmendmentPageResponse, AmendmentError>> GetOwnPageAsync(
        Guid patientCode, AmendmentPageRequest request) =>
        ReadPageAsync(request, (offset, limit) => _repository.GetPageByPatientAsync(patientCode, offset, limit));

    /// <inheritdoc/>
    public Task<Result<AmendmentPageResponse, AmendmentError>> GetPageByPatientAsync(
        Guid patientCode, AmendmentPageRequest request) =>
        _accessPolicy
            .RequireAsync<AmendmentError>(patientCode, PatientDataScope.Clinical,
                error => new AmendmentDataAccessError(error.Message, error.Details, error.Exception),
                new AmendmentAccessDeniedError())
            .BindAsync(decision => ReadPageAsync(request,
                    (offset, limit) => _repository.GetPageByPatientAsync(patientCode, offset, limit))
                .TapAsync(_ => _accessLogger.LogAsync(AccessResourceTypes.Amendments, patientCode,
                    viaBreakGlass: decision == PatientAccessDecision.AllowedViaBreakGlass)));

    /// <inheritdoc/>
    public Task<Result<AmendmentPageResponse, AmendmentError>> GetPageAsync(AmendmentPageRequest request)
    {
        string? status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim();
        if (status is not null && !AmendmentStatuses.All.Contains(status))
            return Fail<AmendmentPageResponse>(new AmendmentInvalidError("El estado indicado no es válido."));

        return ReadPageAsync(request, (offset, limit) => _repository.GetPageAsync(status, offset, limit));
    }

    /// <inheritdoc/>
    public Task<Result<AmendmentResponse, AmendmentError>> DecideAsync(
        Guid amendmentRequestCode, DecideAmendmentRequest request, Guid reviewerUserCode)
    {
        string reason = request.Reason.Trim();
        string? text = string.IsNullOrWhiteSpace(request.AmendmentText) ? null : request.AmendmentText.Trim();

        if (reason.Length < 5)
            return Fail<AmendmentResponse>(new AmendmentInvalidError("Indique el motivo de la decisión."));
        if (request.Accepted && text is null)
            return Fail<AmendmentResponse>(
                new AmendmentInvalidError("Para aceptar, indique el texto que se agregará a la historia."));

        return _repository.GetByCodeAsync(amendmentRequestCode)
            .MapErrorAsync(ToDataError)
            .EnsureNotNullAsync(new AmendmentNotFoundError())
            .EnsureAsync(row => row.Status == AmendmentStatuses.Pending, new AmendmentAlreadyDecidedError())
            .BindAsync(row => _accessPolicy
                .RequireAsync<AmendmentError>(row.PatientCode, PatientDataScope.Clinical,
                    error => new AmendmentDataAccessError(error.Message, error.Details, error.Exception),
                    new AmendmentAccessDeniedError())
                .MapAsync(_ => row))
            .BindAsync(row => _repository
                .DecideAsync(amendmentRequestCode, request.Accepted, reason, text, reviewerUserCode)
                .MapErrorAsync(ToDataError)
                // null here means someone else decided it between the read and the update.
                .EnsureNotNullAsync(new AmendmentAlreadyDecidedError()))
            .TapAsync(row => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.AmendmentDecided, reviewerUserCode, ActivityEntityTypes.AmendmentRequest,
                row.AmendmentRequestCode, "Amendment request decided",
                new { Decision = row.Status, PatientCode = row.PatientCode })))
            .MapAsync(MapToResponse);
    }

    private static Task<Result<AmendmentPageResponse, AmendmentError>> ReadPageAsync(
        AmendmentPageRequest request,
        Func<int, int, Task<Result<IEnumerable<AmendmentRow>, Infrastructure.Core.DTOs.Amendment.AmendmentRepositoryError>>> read)
    {
        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        return read((page - 1) * pageSize, pageSize)
            .MapErrorAsync(ToDataError)
            .MapAsync(rows => BuildPage(rows, page, pageSize));
    }

    private static Task<Result<T, AmendmentError>> Fail<T>(AmendmentError error) =>
        Task.FromResult(Result<T, AmendmentError>.Failure(error));

    private static AmendmentError ToDataError(Infrastructure.Core.DTOs.Amendment.AmendmentRepositoryError error) =>
        new AmendmentDataAccessError(error.Message, error.Details, error.Exception);

    private static AmendmentPageResponse BuildPage(IEnumerable<AmendmentRow> rows, int page, int pageSize)
    {
        List<AmendmentRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;

        return new AmendmentPageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static AmendmentResponse MapToResponse(AmendmentRow row) =>
        new()
        {
            AmendmentRequestCode = row.AmendmentRequestCode,
            PatientCode = row.PatientCode,
            PatientName = row.PatientName,
            MedicalRecordNumber = row.MedicalRecordNumber,
            TargetArea = row.TargetArea,
            RequestedChange = row.RequestedChange,
            Status = row.Status,
            RequestedAt = row.RequestedAt,
            ReviewedByName = row.ReviewedByName,
            ReviewedAt = row.ReviewedAt,
            DecisionReason = row.DecisionReason,
            AmendmentText = row.AmendmentText
        };
}
