using System.Text.Json;
using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Diagnostic.Request;
using Application.Core.DTOs.Diagnostic.Response;
using Application.Core.Interfaces.Diagnostic;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Diagnostic;
using Infrastructure.Core.Models.Diagnostic;

namespace Application.Core.Services.Diagnostic;

public sealed class DiagnosticService : IDiagnostic
{
    private readonly IDiagnosticRepository _repository;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public DiagnosticService(IDiagnosticRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<DiagnosticPageResponse, DiagnosticError>> GetPageAsync(
        Guid patientCode, DiagnosticPageRequest request)
    {
        int offset = (request.Page - 1) * request.PageSize;

        return _repository.GetPageAsync(patientCode, offset, request.PageSize)
            .MapErrorAsync(DiagnosticError (error) => new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));
    }

    /// <inheritdoc/>
    public Task<Result<DiagnosticResponse, DiagnosticError>> CreateAsync(
        CreateDiagnosticRequest request, Guid createdByUserCode) =>
        _repository.InsertAsync(request.PatientCode, request.Description, request.DiagnosedAt, request.Notes, createdByUserCode)
            .MapErrorAsync(DiagnosticError (error) => new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new DiagnosticPatientNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<DiagnosticResponse, DiagnosticError>> UpdateAsync(
        Guid code, UpdateDiagnosticRequest request, Guid modifiedByUserCode) =>
        _repository.UpdateAsync(code, request.Description, request.DiagnosedAt, request.Notes, modifiedByUserCode)
            .MapErrorAsync(DiagnosticError (error) => new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new DiagnosticNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, DiagnosticError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(DiagnosticError (error) => new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new DiagnosticInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, DiagnosticError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code, performedByUserCode)
            .MapErrorAsync(DiagnosticError (error) => new DiagnosticDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new DiagnosticInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    private static DiagnosticPageResponse BuildPageResponse(IEnumerable<DiagnosticRow> rows, int page, int pageSize)
    {
        List<DiagnosticRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new DiagnosticPageResponse
        {
            Items      = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = totalPages
        };
    }

    private static DiagnosticResponse MapToResponse(DiagnosticRow row) =>
        new()
        {
            DiagnosticCode = row.DiagnosticCode,
            PatientCode    = row.PatientCode,
            StatusCode     = row.StatusCode,
            StatusName     = row.StatusName,
            Description    = row.Description,
            DiagnosedAt    = row.DiagnosedAt,
            Notes          = row.Notes,
            CreatedAt      = row.CreatedAt,
            Prescription   = row.PrescriptionSummaryJson is null
                ? null
                : JsonSerializer.Deserialize<PrescriptionSummaryResponse>(row.PrescriptionSummaryJson, JsonOptions)
        };
}
