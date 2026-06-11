namespace Application.Core.DTOs.Diagnostic.Errors;

public abstract record DiagnosticError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record DiagnosticDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : DiagnosticError(Message, Details, Exception);

/// <summary>The patient was not found or is inactive.</summary>
public sealed record DiagnosticPatientNotFoundError()
    : DiagnosticError("El paciente no fue encontrado o está inactivo.");

/// <summary>The diagnostic record does not exist or was deleted.</summary>
public sealed record DiagnosticNotFoundError()
    : DiagnosticError("El diagnóstico no fue encontrado.");

/// <summary>The requested state transition is not valid for the current status.</summary>
public sealed record DiagnosticInvalidTransitionError()
    : DiagnosticError("La transición de estado solicitada no es válida para el estado actual del diagnóstico.");