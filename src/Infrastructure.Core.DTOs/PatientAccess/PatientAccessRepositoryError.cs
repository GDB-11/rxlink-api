namespace Infrastructure.Core.DTOs.PatientAccess;

public abstract record PatientAccessRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetPatientAccessFactsError(string? Details = null, Exception? Exception = null)
    : PatientAccessRepositoryError("Error inesperado al evaluar el acceso al paciente.", Details, Exception);

public sealed record GrantBreakGlassError(string? Details = null, Exception? Exception = null)
    : PatientAccessRepositoryError("Error inesperado al registrar el acceso de emergencia.", Details, Exception);

public sealed record GetBreakGlassReportError(string? Details = null, Exception? Exception = null)
    : PatientAccessRepositoryError("Error inesperado al generar el reporte de accesos de emergencia.", Details, Exception);
