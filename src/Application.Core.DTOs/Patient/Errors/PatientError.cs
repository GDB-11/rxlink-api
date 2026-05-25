namespace Application.Core.DTOs.Patient.Errors;

public abstract record PatientError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record PatientDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : PatientError(Message, Details, Exception);

/// <summary>The target patient does not exist or is already inactive.</summary>
public sealed record PatientNotFoundError()
    : PatientError("El paciente no fue encontrado o ya está inactivo.");
