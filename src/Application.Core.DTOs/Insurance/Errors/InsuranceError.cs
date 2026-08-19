namespace Application.Core.DTOs.Insurance.Errors;

public abstract record InsuranceError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record InsuranceDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : InsuranceError(Message, Details, Exception);

/// <summary>The target Insurance does not exist or is already inactive.</summary>
public sealed record InsuranceNotFoundError()
    : InsuranceError("El seguro no fue encontrado o ya está inactivo.");
