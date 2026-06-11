namespace Application.Core.DTOs.Patient.Errors;

public abstract record PatientError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record PatientDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : PatientError(Message, Details, Exception);

/// <summary>The provided PersonCode does not match any registered person.</summary>
public sealed record PatientPersonNotFoundError()
    : PatientError("La persona seleccionada no fue encontrada. Regístrela primero en el módulo de Personas.");

/// <summary>The target patient does not exist or is already inactive.</summary>
public sealed record PatientNotFoundError()
    : PatientError("El paciente no fue encontrado o ya está inactivo.");

/// <summary>The allergy record does not exist on this patient or was already removed.</summary>
public sealed record PatientAllergyNotFoundError()
    : PatientError("La alergia no fue encontrada en este paciente o ya fue eliminada.");