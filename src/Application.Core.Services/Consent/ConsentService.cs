using Application.Core.DTOs.Consent;
using Application.Core.DTOs.Consent.Errors;
using Application.Core.DTOs.Consent.Request;
using Application.Core.DTOs.Consent.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.Interfaces.Consent;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.PatientAccess;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.DTOs.Consent;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Consent;
using Infrastructure.Core.Models.Consent;

namespace Application.Core.Services.Consent;

public sealed class ConsentService : IConsent
{
    private const int MaxPageSize = 100;

    /// <summary>Tolerance for clock skew between the browser and the server.</summary>
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    private readonly IConsentRepository _repository;
    private readonly IPatientAccessPolicy _accessPolicy;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IActivityLogger _activityLogger;

    public ConsentService(IConsentRepository repository, IPatientAccessPolicy accessPolicy,
        IRecordAccessLogger accessLogger, IActivityLogger activityLogger)
    {
        _repository = repository;
        _accessPolicy = accessPolicy;
        _accessLogger = accessLogger;
        _activityLogger = activityLogger;
    }

    /// <inheritdoc/>
    public Task<Result<ConsentResponse, ConsentError>> RecordAsync(
        Guid patientCode, CreateConsentRequest request, Guid recordedByUserCode)
    {
        string type = request.ConsentType.Trim();
        string description = request.Description.Trim();

        if (!ConsentTypes.All.Contains(type))
            return Fail<ConsentResponse>(new ConsentInvalidError("El tipo de consentimiento no es válido."));
        if (description.Length == 0)
            return Fail<ConsentResponse>(new ConsentInvalidError("La descripción es obligatoria."));
        if (request.GrantedAt > DateTimeOffset.UtcNow + FutureTolerance)
            return Fail<ConsentResponse>(new ConsentInvalidError("La fecha del consentimiento no puede ser futura."));

        return RequireStaffAccessAsync(patientCode)
            .BindAsync(_ => _repository
                .InsertAsync(patientCode, type, description, request.GrantedAt, recordedByUserCode)
                .MapErrorAsync(ToDataError)
                .EnsureNotNullAsync(new ConsentPatientNotFoundError()))
            // Type only: the event never carries the description.
            .TapAsync(row => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.ConsentRecorded, recordedByUserCode, ActivityEntityTypes.Consent,
                row.ConsentCode, "Consent recorded", new { PatientCode = patientCode, ConsentType = type })))
            .MapAsync(MapToResponse);
    }

    /// <inheritdoc/>
    public Task<Result<ConsentResponse, ConsentError>> RevokeAsync(
        Guid patientCode, Guid consentCode, RevokeConsentRequest request, Guid revokedByUserCode)
    {
        string reason = request.Reason.Trim();
        if (reason.Length == 0)
            return Fail<ConsentResponse>(new ConsentInvalidError("El motivo de la revocación es obligatorio."));

        return RequireStaffAccessAsync(patientCode)
            .BindAsync(_ => RevokeCoreAsync(patientCode, consentCode, reason, revokedByUserCode));
    }

    /// <inheritdoc/>
    public Task<Result<ConsentResponse, ConsentError>> RevokeOwnAsync(
        Guid patientCode, Guid consentCode, RevokeConsentRequest request)
    {
        string reason = request.Reason.Trim();
        return reason.Length == 0
            ? Fail<ConsentResponse>(new ConsentInvalidError("El motivo de la revocación es obligatorio."))
            : RevokeCoreAsync(patientCode, consentCode, reason, revokedByUserCode: null);
    }

    /// <inheritdoc/>
    public Task<Result<ConsentPageResponse, ConsentError>> GetPageAsync(Guid patientCode, ConsentPageRequest request) =>
        RequireStaffAccessAsync(patientCode)
            .BindAsync(decision => ReadPageAsync(patientCode, request)
                .TapAsync(_ => _accessLogger.LogAsync(AccessResourceTypes.Consents, patientCode,
                    viaBreakGlass: decision == PatientAccessDecision.AllowedViaBreakGlass)));

    /// <inheritdoc/>
    public Task<Result<ConsentPageResponse, ConsentError>> GetOwnPageAsync(
        Guid patientCode, ConsentPageRequest request) =>
        ReadPageAsync(patientCode, request);

    private Task<Result<PatientAccessDecision, ConsentError>> RequireStaffAccessAsync(Guid patientCode) =>
        _accessPolicy.RequireAsync<ConsentError>(patientCode, PatientDataScope.Administrative,
            error => new ConsentDataAccessError(error.Message, error.Details, error.Exception),
            new ConsentAccessDeniedError());

    private Task<Result<ConsentResponse, ConsentError>> RevokeCoreAsync(
        Guid patientCode, Guid consentCode, string reason, Guid? revokedByUserCode) =>
        _repository.RevokeAsync(patientCode, consentCode, reason, revokedByUserCode)
            .MapErrorAsync(ToDataError)
            .EnsureNotNullAsync(new ConsentNotFoundError())
            // Never the reason: it is free text typed by the patient or staff.
            .TapAsync(row => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.ConsentRevoked, revokedByUserCode, ActivityEntityTypes.Consent,
                row.ConsentCode, "Consent revoked",
                new { PatientCode = patientCode, row.ConsentType, ByPatient = revokedByUserCode is null })))
            .MapAsync(MapToResponse);

    private Task<Result<ConsentPageResponse, ConsentError>> ReadPageAsync(Guid patientCode, ConsentPageRequest request)
    {
        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        return _repository.GetPageAsync(patientCode, (page - 1) * pageSize, pageSize)
            .MapErrorAsync(ToDataError)
            .MapAsync(rows =>
            {
                List<ConsentRow> list = rows.ToList();
                int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
                return new ConsentPageResponse
                {
                    Items = list.Select(MapToResponse).ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            });
    }

    private static Task<Result<T, ConsentError>> Fail<T>(ConsentError error) =>
        Task.FromResult(Result<T, ConsentError>.Failure(error));

    private static ConsentError ToDataError(ConsentRepositoryError error) =>
        new ConsentDataAccessError(error.Message, error.Details, error.Exception);

    private static ConsentResponse MapToResponse(ConsentRow row) =>
        new()
        {
            ConsentCode = row.ConsentCode,
            PatientCode = row.PatientCode,
            ConsentType = row.ConsentType,
            Description = row.Description,
            GrantedAt = row.GrantedAt,
            RecordedByName = row.RecordedByName,
            RecordedAt = row.RecordedAt,
            RevokedAt = row.RevokedAt,
            RevokedByName = row.RevokedByName,
            RevokedReason = row.RevokedReason
        };
}
