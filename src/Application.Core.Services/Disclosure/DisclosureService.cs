using Application.Core.DTOs.Disclosure;
using Application.Core.DTOs.Disclosure.Errors;
using Application.Core.DTOs.Disclosure.Request;
using Application.Core.DTOs.Disclosure.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.Interfaces.Disclosure;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.PatientAccess;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.DTOs.Disclosure;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Disclosure;
using Infrastructure.Core.Models.Disclosure;

namespace Application.Core.Services.Disclosure;

public sealed class DisclosureService : IDisclosure
{
    private const int MaxPageSize = 100;

    /// <summary>Tolerance for clock skew between the browser and the server.</summary>
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    private readonly IDisclosureRepository _repository;
    private readonly IPatientAccessPolicy _accessPolicy;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IActivityLogger _activityLogger;

    public DisclosureService(IDisclosureRepository repository, IPatientAccessPolicy accessPolicy,
        IRecordAccessLogger accessLogger, IActivityLogger activityLogger)
    {
        _repository = repository;
        _accessPolicy = accessPolicy;
        _accessLogger = accessLogger;
        _activityLogger = activityLogger;
    }

    /// <inheritdoc/>
    public Task<Result<DisclosureResponse, DisclosureError>> RecordAsync(
        Guid patientCode, CreateDisclosureRequest request, Guid recordedByUserCode)
    {
        string recipientName = request.RecipientName.Trim();
        string recipientType = request.RecipientType.Trim();
        string purpose = request.Purpose.Trim();
        string description = request.Description.Trim();

        if (!DisclosureRecipientTypes.All.Contains(recipientType))
            return Fail<DisclosureResponse>(new DisclosureInvalidError("El tipo de destinatario no es válido."));
        if (recipientName.Length == 0 || purpose.Length == 0 || description.Length == 0)
            return Fail<DisclosureResponse>(
                new DisclosureInvalidError("Destinatario, motivo y descripción son obligatorios."));
        if (request.DisclosedAt > DateTimeOffset.UtcNow + FutureTolerance)
            return Fail<DisclosureResponse>(new DisclosureInvalidError("La fecha de divulgación no puede ser futura."));

        return _accessPolicy
            .RequireAsync<DisclosureError>(patientCode, PatientDataScope.Administrative,
                error => new DisclosureDataAccessError(error.Message, error.Details, error.Exception),
                new DisclosureAccessDeniedError())
            .BindAsync(_ => _repository
                .InsertAsync(patientCode, request.DisclosedAt, recipientName, recipientType, purpose, description,
                    recordedByUserCode)
                .MapErrorAsync(ToDataError)
                .EnsureNotNullAsync(new DisclosurePatientNotFoundError()))
            // Recipient type only: the event never carries the recipient's name or the description.
            .TapAsync(row => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.DisclosureRecorded, recordedByUserCode, ActivityEntityTypes.Disclosure,
                row.DisclosureCode, "Disclosure recorded",
                new { PatientCode = patientCode, RecipientType = recipientType })))
            .MapAsync(MapToResponse);
    }

    /// <inheritdoc/>
    public Task<Result<DisclosurePageResponse, DisclosureError>> GetPageAsync(
        Guid patientCode, DisclosurePageRequest request) =>
        _accessPolicy
            .RequireAsync<DisclosureError>(patientCode, PatientDataScope.Administrative,
                error => new DisclosureDataAccessError(error.Message, error.Details, error.Exception),
                new DisclosureAccessDeniedError())
            .BindAsync(decision => ReadPageAsync(patientCode, request)
                .TapAsync(_ => _accessLogger.LogAsync(AccessResourceTypes.Disclosures, patientCode,
                    viaBreakGlass: decision == PatientAccessDecision.AllowedViaBreakGlass)));

    /// <inheritdoc/>
    public Task<Result<DisclosurePageResponse, DisclosureError>> GetOwnPageAsync(
        Guid patientCode, DisclosurePageRequest request) =>
        ReadPageAsync(patientCode, request);

    private Task<Result<DisclosurePageResponse, DisclosureError>> ReadPageAsync(
        Guid patientCode, DisclosurePageRequest request)
    {
        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        return _repository.GetPageAsync(patientCode, (page - 1) * pageSize, pageSize)
            .MapErrorAsync(ToDataError)
            .MapAsync(rows => BuildPage(rows, page, pageSize));
    }

    private static Task<Result<T, DisclosureError>> Fail<T>(DisclosureError error) =>
        Task.FromResult(Result<T, DisclosureError>.Failure(error));

    private static DisclosureError ToDataError(DisclosureRepositoryError error) =>
        new DisclosureDataAccessError(error.Message, error.Details, error.Exception);

    private static DisclosurePageResponse BuildPage(IEnumerable<DisclosureRow> rows, int page, int pageSize)
    {
        List<DisclosureRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;

        return new DisclosurePageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static DisclosureResponse MapToResponse(DisclosureRow row) =>
        new()
        {
            DisclosureCode = row.DisclosureCode,
            PatientCode = row.PatientCode,
            DisclosedAt = row.DisclosedAt,
            RecipientName = row.RecipientName,
            RecipientType = row.RecipientType,
            Purpose = row.Purpose,
            Description = row.Description,
            RecordedByName = row.RecordedByName,
            RecordedAt = row.RecordedAt
        };
}
