namespace Application.Core.DTOs.Prescription.Errors;

public abstract record PrescriptionError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record PrescriptionDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : PrescriptionError(Message, Details, Exception);

/// <summary>The prescription was not found or was deleted.</summary>
public sealed record PrescriptionNotFoundError()
    : PrescriptionError("La receta no fue encontrada.");

/// <summary>The diagnostic was not found or is inactive.</summary>
public sealed record PrescriptionDiagnosticNotFoundError()
    : PrescriptionError("El diagnóstico no fue encontrado o está inactivo.");

/// <summary>A non-deleted prescription already exists for this diagnostic.</summary>
public sealed record PrescriptionDuplicateError()
    : PrescriptionError("Ya existe una receta activa para este diagnóstico.");

/// <summary>The prescription is not in a state that allows this operation.</summary>
public sealed record PrescriptionInvalidStatusError()
    : PrescriptionError("La receta no se puede modificar en su estado actual.");

/// <summary>The requested state transition is not valid for the current status.</summary>
public sealed record PrescriptionInvalidTransitionError()
    : PrescriptionError("La transición de estado solicitada no es válida para el estado actual de la receta.");
