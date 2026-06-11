namespace Infrastructure.Core.DTOs.Diagnostic;

public abstract record DiagnosticRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetDiagnosticsPageError(string? Details = null, Exception? Exception = null)
    : DiagnosticRepositoryError("Error inesperado al recuperar los diagnósticos.", Details, Exception);

public sealed record InsertDiagnosticError(string? Details = null, Exception? Exception = null)
    : DiagnosticRepositoryError("Error inesperado al registrar el diagnóstico.", Details, Exception);

/// <summary>Unique constraint uq_diagnostic_appointment was violated.</summary>
public sealed record InsertDiagnosticDuplicateError(string? Details = null, Exception? Exception = null)
    : DiagnosticRepositoryError("Ya existe un diagnóstico activo para esta cita.", Details, Exception);

public sealed record UpdateDiagnosticError(string? Details = null, Exception? Exception = null)
    : DiagnosticRepositoryError("Error inesperado al actualizar el diagnóstico.", Details, Exception);

public sealed record DeactivateDiagnosticError(string? Details = null, Exception? Exception = null)
    : DiagnosticRepositoryError("Error inesperado al cambiar el estado del diagnóstico.", Details, Exception);