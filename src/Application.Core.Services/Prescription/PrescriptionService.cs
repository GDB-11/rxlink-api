using System.Text.Json;
using Application.Core.DTOs.Prescription.Errors;
using Application.Core.Interfaces.Catalog;
using Application.Core.DTOs.Prescription.Request;
using Application.Core.DTOs.Prescription.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Interfaces.Prescription;
using Application.Core.Services.PatientAccess;
using Infrastructure.Core.Interfaces.Activity;
using BindSharp;
using BindSharp.Extensions;
using Common.Helpers;
using Infrastructure.Core.DTOs.Prescription;
using Infrastructure.Core.Interfaces.Prescription;
using Infrastructure.Core.Models.Prescription;

namespace Application.Core.Services.Prescription;

public sealed class PrescriptionService : IPrescription
{
    private readonly IPrescriptionRepository _repository;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IPatientAccessPolicy _accessPolicy;
    private readonly ICatalogWriteValidator _catalog;
    private readonly PrescriptionLineSnapshotBuilder _snapshots;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public PrescriptionService(IPrescriptionRepository repository, IRecordAccessLogger accessLogger,
        IPatientAccessPolicy accessPolicy, ICatalogWriteValidator catalog,
        PrescriptionLineSnapshotBuilder snapshots)
    {
        _repository = repository;
        _accessLogger = accessLogger;
        _accessPolicy = accessPolicy;
        _catalog = catalog;
        _snapshots = snapshots;
    }

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<DoctorDraftPrescriptionResponse>, PrescriptionError>>
        GetDoctorDraftPrescriptionsAsync(
            Guid doctorUserCode) =>
        _repository.GetDoctorDraftPrescriptionsAsync(doctorUserCode)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => (IReadOnlyList<DoctorDraftPrescriptionResponse>)rows
                .Select(r => new DoctorDraftPrescriptionResponse
                {
                    PrescriptionCode = r.PrescriptionCode,
                    PatientCode = r.PatientCode,
                    PatientNames = r.PatientNames,
                    PatientSurnames = r.PatientSurnames,
                    DiagnosticDescription = r.DiagnosticDescription,
                    CreatedAt = r.CreatedAt,
                    DetailCount = r.DetailCount
                })
                .ToList());

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<NurseDispensationResponse>, PrescriptionError>> GetNurseDispensationsByDateAsync(
        Guid nurseUserCode, DateOnly date) =>
        _repository.GetNurseDispensationsByDateAsync(nurseUserCode, date.ToDateTime())
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => (IReadOnlyList<NurseDispensationResponse>)rows
                .Select(r => new NurseDispensationResponse
                {
                    PrescriptionCode = r.PrescriptionCode,
                    PatientCode = r.PatientCode,
                    PatientNames = r.PatientNames,
                    PatientSurnames = r.PatientSurnames,
                    DiagnosticDescription = r.DiagnosticDescription,
                    DispensedAt = r.DispensedAt,
                    DetailCount = r.DetailCount,
                    MedicationNames = r.MedicationNames
                })
                .ToList());

    /// <inheritdoc/>
    public Task<Result<PrescriptionResponse, PrescriptionError>> CreateAsync(
        CreatePrescriptionRequest request, Guid createdByUserCode) =>
        ValidateLinesAsync(request.Details)
            .BindAsync(detailsJson => _repository.InsertAsync(request.DiagnosticCode, request.Notes,
                request.ValidUntil.ToDateTime(), detailsJson, createdByUserCode)
            .MapErrorAsync(PrescriptionError (error) => error switch
            {
                InsertPrescriptionDuplicateError => new PrescriptionDuplicateError(),
                _ => new PrescriptionDataAccessError(error.Message, error.Details, error.Exception)
            })
            .EnsureNotNullAsync(new PrescriptionDiagnosticNotFoundError())
            .MapAsync(MapToResponse));

    /// <inheritdoc/>
    public Task<Result<PrescriptionResponse, PrescriptionError>> GetAsync(Guid code) =>
        _repository.GetByCodeAsync(code)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PrescriptionNotFoundError())
            .MapAsync(MapToResponse)
            .BindAsync(response => _accessPolicy
                .RequireAsync<PrescriptionError>(response.PatientCode, PatientDataScope.Clinical,
                    error => new PrescriptionDataAccessError(error.Message, error.Details, error.Exception),
                    new PrescriptionPatientForbiddenError())
                .TapAsync(decision => _accessLogger.LogAsync(AccessResourceTypes.Prescription,
                    response.PatientCode, code, decision == PatientAccessDecision.AllowedViaBreakGlass))
                .MapAsync(_ => response));

    /// <inheritdoc/>
    public Task<Result<PrescriptionResponse, PrescriptionError>> GetForPatientAsync(Guid code, Guid patientCode) =>
        _repository.GetByCodeAsync(code)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PrescriptionNotFoundError())
            .MapAsync(MapToResponse)
            .EnsureAsync(r => r.PatientCode == patientCode, new PrescriptionPatientForbiddenError())
            .TapAsync(r => _accessLogger.LogAsync(AccessResourceTypes.Prescription, r.PatientCode, code));

    /// <inheritdoc/>
    public Task<Result<PrescriptionResponse, PrescriptionError>> UpdateAsync(
        Guid code, UpdatePrescriptionRequest request, Guid modifiedByUserCode) =>
        ValidateLinesAsync(request.Details)
            .BindAsync(detailsJson => _repository.UpdateAsync(code, request.Notes, request.ValidUntil.ToDateTime(),
                detailsJson, modifiedByUserCode)
            .MapErrorAsync(PrescriptionError (error) => error switch
            {
                UpdatePrescriptionInvalidStatusError => new PrescriptionInvalidStatusError(),
                _ => new PrescriptionDataAccessError(error.Message, error.Details, error.Exception)
            })
            .EnsureNotNullAsync(new PrescriptionNotFoundError())
            .MapAsync(MapToResponse));

    /// <summary>Validates the catalog codes and returns the lines as JSON with the §6.2 snapshot values attached.</summary>
    private async Task<Result<string, PrescriptionError>> ValidateLinesAsync(
        IReadOnlyList<PrescriptionDetailRequest> details)
    {
        Result<Unit, Application.Core.DTOs.Catalog.Errors.CatalogValidationError> validation =
            await _catalog.ValidatePrescriptionLinesAsync(details
                .Select(d => (d.MedicationCode, d.AdministrationRouteCode, d.FrequencyCode)).ToList());
        if (!validation.IsSuccess)
            return Result<string, PrescriptionError>.Failure(new PrescriptionCatalogValidationError(validation.Error));

        Result<string, Application.Core.DTOs.Catalog.Errors.CatalogValidationError> json = _snapshots.BuildJson(details);
        return json.IsSuccess
            ? Result<string, PrescriptionError>.Success(json.Value)
            : Result<string, PrescriptionError>.Failure(new PrescriptionCatalogValidationError(json.Error));
    }

    /// <inheritdoc/>
    public Task<Result<Unit, PrescriptionError>> SignAsync(Guid code, Guid performedByUserCode) =>
        _repository.SignAsync(code, performedByUserCode)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PrescriptionInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, PrescriptionError>> SuspendAsync(Guid code, Guid performedByUserCode) =>
        _repository.SuspendAsync(code, performedByUserCode)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PrescriptionInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, PrescriptionError>> ReactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ReactivateAsync(code, performedByUserCode)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PrescriptionInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, PrescriptionError>> CancelAsync(Guid code, Guid performedByUserCode) =>
        _repository.CancelAsync(code, performedByUserCode)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PrescriptionInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, PrescriptionError>> DispenseAsync(
        Guid code, DispensePrescriptionRequest request, Guid performedByUserCode) =>
        _repository.DispenseAsync(code, performedByUserCode,
                request.Items.Select(i => new DispenseLineRow(i.PrescriptionDetailCode, i.Quantity)).ToList(),
                string.IsNullOrWhiteSpace(request.OverrideReason) ? null : request.OverrideReason.Trim())
            .MapErrorAsync(PrescriptionError (error) => error switch
            {
                DispenseLinesInvalidError => new PrescriptionDispenseLinesInvalidError(),
                InsufficientStockError => new PrescriptionInsufficientStockError(),
                _ => new PrescriptionDataAccessError(error.Message, error.Details, error.Exception)
            })
            .EnsureAsync(affected => affected > 0, new PrescriptionInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    private static PrescriptionResponse MapToResponse(PrescriptionRow row)
    {
        List<PrescriptionDetailResponse> details =
            JsonSerializer.Deserialize<List<PrescriptionDetailResponse>>(row.DetailsJson, JsonOptions)
            ?? [];

        return new PrescriptionResponse
        {
            PrescriptionCode = row.PrescriptionCode,
            DiagnosticCode = row.DiagnosticCode,
            DiagnosticDescription = row.DiagnosticDescription,
            PatientCode = row.PatientCode,
            StatusName = row.StatusName,
            StatusCode = row.StatusCode,
            Notes = row.Notes,
            ValidUntil = row.ValidUntil,
            CreatedAt = row.CreatedAt,
            Details = details
        };
    }
}