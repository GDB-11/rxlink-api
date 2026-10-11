namespace Application.Core.DTOs.Report.Errors;

public abstract record ReportError(string Message, string? Details = null, Exception? Exception = null);

public sealed record ReportDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : ReportError(Message, Details, Exception);

public sealed record ReportInvalidRangeError()
    : ReportError("El rango de fechas es inválido: 'from' debe ser menor o igual a 'to' y el rango no puede superar 366 días.");
