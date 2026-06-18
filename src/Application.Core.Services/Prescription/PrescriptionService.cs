using System.Text.Json;
using Application.Core.DTOs.Prescription.Errors;
using Application.Core.DTOs.Prescription.Request;
using Application.Core.DTOs.Prescription.Response;
using Application.Core.Interfaces.Prescription;
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

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public PrescriptionService(IPrescriptionRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<PrescriptionResponse, PrescriptionError>> CreateAsync(
        CreatePrescriptionRequest request, Guid createdByUserCode) =>
        _repository.InsertAsync(request.DiagnosticCode, request.Notes, request.ValidUntil.ToDateTime(),
                JsonSerializer.Serialize(request.Details), createdByUserCode)
            .MapErrorAsync(PrescriptionError (error) => error switch
            {
                InsertPrescriptionDuplicateError => new PrescriptionDuplicateError(),
                _ => new PrescriptionDataAccessError(error.Message, error.Details, error.Exception)
            })
            .EnsureNotNullAsync(new PrescriptionDiagnosticNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<PrescriptionResponse, PrescriptionError>> GetAsync(Guid code) =>
        _repository.GetByCodeAsync(code)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PrescriptionNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<PrescriptionResponse, PrescriptionError>> GetForPatientAsync(Guid code, Guid patientCode) =>
        _repository.GetByCodeAsync(code)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PrescriptionNotFoundError())
            .MapAsync(MapToResponse)
            .EnsureAsync(r => r.PatientCode == patientCode, new PrescriptionPatientForbiddenError());

    /// <inheritdoc/>
    public Task<Result<PrescriptionResponse, PrescriptionError>> UpdateAsync(
        Guid code, UpdatePrescriptionRequest request, Guid modifiedByUserCode) =>
        _repository.UpdateAsync(code, request.Notes, request.ValidUntil.ToDateTime(), JsonSerializer.Serialize(request.Details),
                modifiedByUserCode)
            .MapErrorAsync(PrescriptionError (error) => error switch
            {
                UpdatePrescriptionInvalidStatusError => new PrescriptionInvalidStatusError(),
                _ => new PrescriptionDataAccessError(error.Message, error.Details, error.Exception)
            })
            .EnsureNotNullAsync(new PrescriptionNotFoundError())
            .MapAsync(MapToResponse);

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
    public Task<Result<Unit, PrescriptionError>> DispenseAsync(Guid code, Guid performedByUserCode) =>
        _repository.DispenseAsync(code, performedByUserCode)
            .MapErrorAsync(PrescriptionError (error) =>
                new PrescriptionDataAccessError(error.Message, error.Details, error.Exception))
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