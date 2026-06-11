using BindSharp;
using Infrastructure.Core.DTOs.Diagnostic;
using Infrastructure.Core.Models.Diagnostic;

namespace Infrastructure.Core.Interfaces.Diagnostic;

public interface IDiagnosticRepository
{
    /// <summary>Returns one page of diagnostics for a patient, with a total count via window function.</summary>
    Task<Result<IEnumerable<DiagnosticRow>, DiagnosticRepositoryError>> GetPageAsync(
        Guid patientCode, int offset, int limit);

    /// <summary>
    /// Inserts a new diagnostic with status Activo.
    /// Returns null when the patient is not found or inactive.
    /// </summary>
    Task<Result<DiagnosticRow?, DiagnosticRepositoryError>> InsertAsync(
        Guid patientCode, string description, DateOnly diagnosedAt, string? notes, Guid createdByUserCode);

    /// <summary>
    /// Updates description, date and notes.
    /// Returns null when the diagnostic is not found or was deleted.
    /// </summary>
    Task<Result<DiagnosticRow?, DiagnosticRepositoryError>> UpdateAsync(
        Guid code, string description, DateOnly diagnosedAt, string? notes, Guid modifiedByUserCode);

    /// <summary>
    /// Transitions Activo → Inactivo.
    /// Returns 0 when the diagnostic is not found, already Inactivo, or deleted.
    /// </summary>
    Task<Result<int, DiagnosticRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>
    /// Transitions Inactivo → Activo.
    /// Returns 0 when the diagnostic is not found, already Activo, or deleted.
    /// </summary>
    Task<Result<int, DiagnosticRepositoryError>> ActivateAsync(Guid code, Guid performedByUserCode);
}