namespace Application.Core.DTOs.Diagnostic.Errors;

public abstract record DiagnosticError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record DiagnosticDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : DiagnosticError(Message, Details, Exception);

/// <summary>The diagnostic record does not exist or was deleted.</summary>
public sealed record DiagnosticNotFoundError()
    : DiagnosticError("El diagnóstico no fue encontrado.");

/// <summary>
/// The appointment was not found or its status does not allow creating a diagnostic
/// (must be Confirmado or Completado).
/// </summary>
public sealed record DiagnosticInvalidAppointmentError()
    : DiagnosticError("La cita no fue encontrada o no se encuentra en un estado válido para crear un diagnóstico.");

/// <summary>A non-deleted diagnostic already exists for this appointment.</summary>
public sealed record DiagnosticDuplicateError()
    : DiagnosticError("Ya existe un diagnóstico activo para esta cita.");

/// <summary>The requested state transition is not valid for the current status.</summary>
public sealed record DiagnosticInvalidTransitionError()
    : DiagnosticError("La transición de estado solicitada no es válida para el estado actual del diagnóstico.");