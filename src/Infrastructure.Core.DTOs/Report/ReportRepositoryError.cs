namespace Infrastructure.Core.DTOs.Report;

public abstract record ReportRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetActivityByUserError(string? Details = null, Exception? Exception = null)
    : ReportRepositoryError("Error inesperado al generar el reporte de actividad por usuario.", Details, Exception);

public sealed record GetUnattributedWritesError(string? Details = null, Exception? Exception = null)
    : ReportRepositoryError("Error inesperado al generar el reporte de escrituras sin actor.", Details, Exception);
