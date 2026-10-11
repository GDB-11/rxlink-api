namespace Application.Core.DTOs.PatientAccess.Errors;

public abstract record PatientAccessError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record PatientAccessDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : PatientAccessError(Message, Details, Exception);

/// <summary>The caller's role may not use this operation.</summary>
public sealed record PatientAccessNotPermittedError()
    : PatientAccessError("No tiene permiso para realizar esta operación.");

/// <summary>No active patient matches the medical record number.</summary>
public sealed record BreakGlassPatientNotFoundError()
    : PatientAccessError("No se encontró un paciente activo con ese número de historia clínica.");

/// <summary>The report date range is inverted or too large.</summary>
public sealed record BreakGlassReportInvalidRangeError()
    : PatientAccessError("El rango de fechas es inválido: 'from' debe ser menor o igual a 'to' y el rango no puede superar 366 días.");
