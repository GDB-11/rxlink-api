using System.Text.Json;
using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Diagnostic.Request;
using Application.Core.DTOs.Diagnostic.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.Interfaces.Diagnostic;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.PatientAccess;
using Infrastructure.Core.Interfaces.Activity;
using BindSharp;
using BindSharp.Extensions;
using Common.Helpers;
using Infrastructure.Core.DTOs.Diagnostic;
using Infrastructure.Core.Interfaces.Diagnostic;
using Infrastructure.Core.Models.Diagnostic;

namespace Application.Core.Services.Diagnostic;

public sealed class DiagnosticService : IDiagnostic
{
    private readonly IDiagnosticRepository _repository;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IPatientAccessPolicy _accessPolicy;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public DiagnosticService(IDiagnosticRepository repository, IRecordAccessLogger accessLogger,
        IPatientAccessPolicy accessPolicy)
    {
        _repository = repository;
        _accessLogger = accessLogger;
        _accessPolicy = accessPolicy;
    }

    /// <inheritdoc/>
    public Task<Result<DiagnosticPageResponse, DiagnosticError>> GetPageAsync(
        Guid patientCode, DiagnosticPageRequest request)
    {
        int offset = (request.Page - 1) * request.PageSize;

        return _accessPolicy
            .RequireAsync<DiagnosticError>(patientCode, PatientDataScope.Clinical,
                error => new DiagnosticDataAccessError(error.Message, error.Details, error.Exception),
                new DiagnosticAccessDeniedError())
            .BindAsync(decision => _repository.GetPageAsync(patientCode, offset, request.PageSize)
                .MapErrorAsync(DiagnosticError (error) =>
                    new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
                .TapAsync(_ => _accessLogger.LogAsync(AccessResourceTypes.Diagnostics, patientCode,
                    viaBreakGlass: decision == PatientAccessDecision.AllowedViaBreakGlass))
                .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize)));
    }

    /// <inheritdoc/>
    public Task<Result<DiagnosticResponse, DiagnosticError>> CreateAsync(
        CreateDiagnosticRequest request, Guid createdByUserCode) =>
        _repository.InsertAsync(
                request.AppointmentCode,
                request.Description,
                request.DiagnosedAt.ToDateTime(),
                request.Notes,
                createdByUserCode)
            .MapErrorAsync(DiagnosticError (error) => error switch
            {
                InsertDiagnosticDuplicateError => new DiagnosticDuplicateError(),
                var e => new DiagnosticDataAccessError(e.Message, e.Details, e.Exception)
            })
            .EnsureNotNullAsync(new DiagnosticInvalidAppointmentError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<DiagnosticResponse, DiagnosticError>> UpdateAsync(
        Guid code, UpdateDiagnosticRequest request, Guid modifiedByUserCode) =>
        _repository.UpdateAsync(code, request.Description, request.DiagnosedAt.ToDateTime(), request.Notes, modifiedByUserCode)
            .MapErrorAsync(DiagnosticError (error) =>
                new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new DiagnosticNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, DiagnosticError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(DiagnosticError (error) =>
                new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new DiagnosticInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, DiagnosticError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code, performedByUserCode)
            .MapErrorAsync(DiagnosticError (error) =>
                new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new DiagnosticInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    private static DiagnosticPageResponse BuildPageResponse(IEnumerable<DiagnosticRow> rows, int page, int pageSize)
    {
        List<DiagnosticRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new DiagnosticPageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static DiagnosticResponse MapToResponse(DiagnosticRow row) =>
        new()
        {
            DiagnosticCode = row.DiagnosticCode,
            AppointmentCode = row.AppointmentCode,
            PatientCode = row.PatientCode,
            StatusCode = row.StatusCode,
            StatusName = row.StatusName,
            Description = row.Description,
            DiagnosedAt = row.DiagnosedAt,
            Notes = row.Notes,
            CreatedAt = row.CreatedAt,
            Prescription = row.PrescriptionSummaryJson is null
                ? null
                : JsonSerializer.Deserialize<PrescriptionSummaryResponse>(row.PrescriptionSummaryJson, JsonOptions)
        };
}