namespace Application.Core.DTOs.Medication.Errors;

public abstract record MedicationError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record MedicationDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : MedicationError(Message, Details, Exception);

/// <summary>The target medication does not exist or is already inactive.</summary>
public sealed record MedicationNotFoundError()
    : MedicationError("El medicamento no fue encontrado o ya está inactivo.");