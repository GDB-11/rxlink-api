namespace Application.Core.DTOs.Specialty.Errors;

public abstract record SpecialtyError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record SpecialtyDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : SpecialtyError(Message, Details, Exception);

/// <summary>The target Specialty does not exist or is already inactive.</summary>
public sealed record SpecialtyNotFoundError()
    : SpecialtyError("La especialidad no fue encontrada o ya está inactiva.");