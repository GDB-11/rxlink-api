using Application.Core.Interfaces.Catalog;
using System.Text.Json;
using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Patient.Request;
using Application.Core.DTOs.Patient.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.Interfaces.Patient;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.PatientAccess;
using Infrastructure.Core.Interfaces.Activity;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Patient;
using Infrastructure.Core.Models.Patient;

namespace Application.Core.Services.Patient;

public sealed class PatientService : IPatient
{
    private readonly IPatientRepository _repository;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IPatientAccessPolicy _accessPolicy;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly ICatalogWriteValidator _catalog;

    public PatientService(IPatientRepository repository, IRecordAccessLogger accessLogger,
        IPatientAccessPolicy accessPolicy, ICatalogWriteValidator catalog)
    {
        _repository = repository;
        _accessLogger = accessLogger;
        _accessPolicy = accessPolicy;
        _catalog = catalog;
    }

    /// <inheritdoc/>
    public Task<Result<PatientPageResponse, PatientError>> GetPageAsync(PatientPageRequest request) =>
        _accessPolicy.GetListScopeAsync()
            .MapErrorAsync(ToDataError)
            .BindAsync(scope => _repository
                .GetPageAsync((request.Page - 1) * request.PageSize, request.PageSize, request.Search,
                    scope.RestrictToCareTeamOfUserCode)
                .MapErrorAsync(PatientError (error) =>
                    new PatientDataAccessError(error.Message, error.Details, error.Exception))
                .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize, scope.IncludeClinical)));

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> CreateAsync(CreatePatientRequest request)
    {
        string allergiesJson = JsonSerializer.Serialize(
            request.Allergies.Select(a => new
                { AllergyCode = a.AllergyCode, SeverityCode = a.SeverityCode, Notes = a.Notes }));

        return ValidateAllergiesAsync(request.Allergies.Select(a => (a.AllergyCode, a.SeverityCode)).ToList())
            .BindAsync(_ => _repository.InsertAsync(request.PersonCode, allergiesJson)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientPersonNotFoundError())
            .MapAsync(MapToResponse));
    }

    private Task<Result<Unit, PatientError>> ValidateAllergiesAsync(
        IReadOnlyCollection<(Guid AllergyCode, Guid SeverityCode)> allergies) =>
        _catalog.ValidateAllergiesAsync(allergies)
            .MapErrorAsync(PatientError (error) => new PatientCatalogValidationError(error));

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> UpdateAsync(Guid code, UpdatePatientRequest request) =>
        _repository.UpdateAsync(code, request.MedicalRecordNumber)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> GetSelfAsync(Guid patientCode) =>
        _repository.GetByCodeAsync(patientCode)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .BindAsync(AuthorizeAndMapAsync);

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> GetByPersonCodeAsync(Guid personCode) =>
        _repository.GetByPersonCodeAsync(personCode)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .BindAsync(AuthorizeAndMapAsync);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> UpdateSelfAsync(Guid patientCode, UpdatePatientSelfRequest request) =>
        _repository.UpdatePersonContactAsync(
                patientCode,
                request.Phone,
                request.AlternativePhone,
                request.Address,
                request.EmergencyContactName,
                request.EmergencyContactPhone)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<PatientAllergyResponse, PatientError>> AddAllergyAsync(
        Guid patientCode, PatientAllergyRequest request) =>
        ValidateAllergiesAsync([(request.AllergyCode, request.SeverityCode)])
            .BindAsync(_ => _repository.AddAllergyAsync(patientCode, request.AllergyCode, request.SeverityCode, request.Notes)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .MapAsync(MapAllergyToResponse));

    /// <inheritdoc/>
    public Task<Result<PatientAllergyResponse, PatientError>> UpdateAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, PatientAllergyRequest request) =>
        ValidateAllergiesAsync([(request.AllergyCode, request.SeverityCode)])
            .BindAsync(_ => _repository.UpdateAllergyAsync(patientCode, patientAllergyCode, request.SeverityCode, request.Notes)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientAllergyNotFoundError())
            .MapAsync(MapAllergyToResponse));

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> RemoveAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, Guid performedByUserCode) =>
        _repository.DeleteAllergyAsync(patientCode, patientAllergyCode, performedByUserCode)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientAllergyNotFoundError())
            .MapAsync(_ => Unit.Value);

    private static PatientError ToDataError(PatientAccessError error) =>
        new PatientDataAccessError(error.Message, error.Details, error.Exception);

    /// <summary>
    /// Identity data needs an administrative-scope allow; allergies are only included when the clinical scope
    /// is allowed too. The read is logged after both checks and before the response (fail-closed).
    /// </summary>
    private Task<Result<PatientResponse, PatientError>> AuthorizeAndMapAsync(PatientRow row) =>
        _accessPolicy
            .RequireAsync<PatientError>(row.PatientCode, PatientDataScope.Administrative, ToDataError,
                new PatientAccessDeniedError())
            .BindAsync(decision => _accessPolicy.EvaluateAsync(row.PatientCode, PatientDataScope.Clinical)
                .MapErrorAsync(ToDataError)
                .TapAsync(_ => _accessLogger.LogAsync(AccessResourceTypes.PatientProfile, row.PatientCode,
                    viaBreakGlass: decision == PatientAccessDecision.AllowedViaBreakGlass))
                .MapAsync(clinical => MapToResponse(row, clinical != PatientAccessDecision.Denied)));

    private static PatientPageResponse BuildPageResponse(
        IEnumerable<PatientRow> rows, int page, int pageSize, bool includeClinical)
    {
        List<PatientRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PatientPageResponse
        {
            Items = list.Select(r => MapToResponse(r, includeClinical)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static PatientAllergyResponse MapAllergyToResponse(PatientAllergyRow row) =>
        new()
        {
            PatientAllergyCode = row.PatientAllergyCode,
            AllergyCode = row.AllergyCode,
            AllergyName = row.AllergyName,
            SeverityCode = row.SeverityCode,
            SeverityName = row.SeverityName,
            Notes = row.Notes
        };

    private static PatientResponse MapToResponse(PatientRow row) => MapToResponse(row, includeClinical: true);

    private static PatientResponse MapToResponse(PatientRow row, bool includeClinical) =>
        new()
        {
            PatientCode = row.PatientCode,
            PersonCode = row.PersonCode,
            MedicalRecordNumber = row.MedicalRecordNumber,
            IsActive = row.IsActive,
            Names = row.Names,
            Surnames = row.Surnames,
            BirthDate = row.BirthDate,
            Phone = row.Phone,
            AlternativePhone = row.AlternativePhone,
            Email = row.Email,
            Address = row.Address,
            EmergencyContactName = row.EmergencyContactName,
            EmergencyContactPhone = row.EmergencyContactPhone,
            Allergies = includeClinical
                ? JsonSerializer.Deserialize<List<PatientAllergyResponse>>(row.AllergiesJson, JsonOptions) ?? []
                : []
        };
}