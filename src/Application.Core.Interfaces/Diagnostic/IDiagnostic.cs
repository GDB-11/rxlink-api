using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Diagnostic.Request;
using Application.Core.DTOs.Diagnostic.Response;
using BindSharp;

namespace Application.Core.Interfaces.Diagnostic;

public interface IDiagnostic
{
    /// <summary>Returns a paginated list of diagnostics for a patient.</summary>
    Task<Result<DiagnosticPageResponse, DiagnosticError>> GetPageAsync(
        Guid patientCode, DiagnosticPageRequest request);

    /// <summary>Creates a new diagnostic (initial status: Activo).</summary>
    Task<Result<DiagnosticResponse, DiagnosticError>> CreateAsync(
        CreateDiagnosticRequest request, Guid createdByUserCode);

    /// <summary>Updates the description, date and notes of an existing diagnostic.</summary>
    Task<Result<DiagnosticResponse, DiagnosticError>> UpdateAsync(
        Guid code, UpdateDiagnosticRequest request, Guid modifiedByUserCode);

    /// <summary>Transitions a diagnostic from Activo to Inactivo.</summary>
    Task<Result<Unit, DiagnosticError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions a diagnostic from Inactivo to Activo.</summary>
    Task<Result<Unit, DiagnosticError>> ActivateAsync(Guid code, Guid performedByUserCode);
}