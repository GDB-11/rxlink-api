using System.Text.RegularExpressions;
using Application.Core.DTOs.CommunicationPreference.Errors;
using Application.Core.DTOs.CommunicationPreference.Request;
using Application.Core.DTOs.CommunicationPreference.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.Interfaces.CommunicationPreference;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.PatientAccess;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.DTOs.CommunicationPreference;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.CommunicationPreference;
using Infrastructure.Core.Models.CommunicationPreference;

namespace Application.Core.Services.CommunicationPreference;

public sealed partial class CommunicationPreferenceService : ICommunicationPreference
{
    private readonly ICommunicationPreferenceRepository _repository;
    private readonly IPatientAccessPolicy _accessPolicy;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IActivityLogger _activityLogger;

    public CommunicationPreferenceService(ICommunicationPreferenceRepository repository,
        IPatientAccessPolicy accessPolicy, IRecordAccessLogger accessLogger, IActivityLogger activityLogger)
    {
        _repository = repository;
        _accessPolicy = accessPolicy;
        _accessLogger = accessLogger;
        _activityLogger = activityLogger;
    }

    // Same shapes as the CHECK constraints in V039.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\+?[0-9 ]{6,19}$")]
    private static partial Regex PhonePattern();

    /// <inheritdoc/>
    public Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> GetAsync(Guid patientCode) =>
        _accessPolicy
            .RequireAsync<CommunicationPreferenceError>(patientCode, PatientDataScope.Administrative,
                error => new CommunicationPreferenceDataAccessError(error.Message, error.Details, error.Exception),
                new CommunicationPreferenceAccessDeniedError())
            .BindAsync(decision => ReadAsync(patientCode)
                .TapAsync(_ => _accessLogger.LogAsync(AccessResourceTypes.CommunicationPreferences, patientCode,
                    viaBreakGlass: decision == PatientAccessDecision.AllowedViaBreakGlass)));

    /// <inheritdoc/>
    public Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> UpdateAsync(
        Guid patientCode, UpdateCommunicationPreferenceRequest request, Guid updatedByUserCode)
    {
        Result<ValidRequest, CommunicationPreferenceError> valid = Validate(request);
        if (!valid.IsSuccess)
            return Task.FromResult(Result<CommunicationPreferenceResponse, CommunicationPreferenceError>
                .Failure(valid.Error!));

        return _accessPolicy
            .RequireAsync<CommunicationPreferenceError>(patientCode, PatientDataScope.Administrative,
                error => new CommunicationPreferenceDataAccessError(error.Message, error.Details, error.Exception),
                new CommunicationPreferenceAccessDeniedError())
            .BindAsync(_ => SaveAsync(patientCode, valid.Value!, updatedByUserCode));
    }

    /// <inheritdoc/>
    public Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> GetOwnAsync(Guid patientCode) =>
        ReadAsync(patientCode);

    /// <inheritdoc/>
    public Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> UpdateOwnAsync(
        Guid patientCode, UpdateCommunicationPreferenceRequest request)
    {
        Result<ValidRequest, CommunicationPreferenceError> valid = Validate(request);
        return valid.IsSuccess
            ? SaveAsync(patientCode, valid.Value!, actorUserCode: null)
            : Task.FromResult(Result<CommunicationPreferenceResponse, CommunicationPreferenceError>
                .Failure(valid.Error!));
    }

    private Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> ReadAsync(Guid patientCode) =>
        _repository.GetAsync(patientCode)
            .MapErrorAsync(ToDataError)
            .EnsureNotNullAsync(new CommunicationPreferencePatientNotFoundError())
            .MapAsync(MapToResponse);

    private Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> SaveAsync(
        Guid patientCode, ValidRequest request, Guid? actorUserCode) =>
        _repository
            .UpsertAsync(patientCode, request.AllowEmail, request.AllowSms, request.AlternativeEmail,
                request.AlternativePhone)
            .MapErrorAsync(ToDataError)
            .EnsureNotNullAsync(new CommunicationPreferencePatientNotFoundError())
            // Flags only: the event never carries the alternative e-mail or phone.
            .TapAsync(_ => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.CommunicationPreferencesUpdated, actorUserCode, ActivityEntityTypes.Patient,
                patientCode, "Communication preferences updated",
                new
                {
                    request.AllowEmail,
                    request.AllowSms,
                    HasAlternativeEmail = request.AlternativeEmail is not null,
                    HasAlternativePhone = request.AlternativePhone is not null
                })))
            .MapAsync(MapToResponse);

    private static Result<ValidRequest, CommunicationPreferenceError> Validate(UpdateCommunicationPreferenceRequest request)
    {
        string? email = NullIfBlank(request.AlternativeEmail);
        string? phone = NullIfBlank(request.AlternativePhone);

        if (email is not null && !EmailPattern().IsMatch(email))
            return Result<ValidRequest, CommunicationPreferenceError>.Failure(
                new CommunicationPreferenceInvalidError("El correo alternativo no es válido."));
        if (phone is not null && !PhonePattern().IsMatch(phone))
            return Result<ValidRequest, CommunicationPreferenceError>.Failure(
                new CommunicationPreferenceInvalidError("El teléfono alternativo no es válido."));

        return Result<ValidRequest, CommunicationPreferenceError>.Success(
            new ValidRequest(request.AllowEmail, request.AllowSms, email, phone));
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CommunicationPreferenceError ToDataError(CommunicationPreferenceRepositoryError error) =>
        new CommunicationPreferenceDataAccessError(error.Message, error.Details, error.Exception);

    private static CommunicationPreferenceResponse MapToResponse(CommunicationPreferenceRow row) =>
        new()
        {
            PatientCode = row.PatientCode,
            AllowEmail = row.AllowEmail,
            AllowSms = row.AllowSms,
            AlternativeEmail = row.AlternativeEmail,
            AlternativePhone = row.AlternativePhone,
            UpdatedAt = row.UpdatedAt
        };

    private sealed record ValidRequest(bool AllowEmail, bool AllowSms, string? AlternativeEmail, string? AlternativePhone);
}
