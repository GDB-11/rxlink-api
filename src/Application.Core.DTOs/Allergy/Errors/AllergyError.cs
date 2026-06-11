namespace Application.Core.DTOs.Allergy.Errors;

public abstract record AllergyError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record AllergyDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : AllergyError(Message, Details, Exception);

/// <summary>The target allergy does not exist or is already inactive.</summary>
public sealed record AllergyNotFoundError()
    : AllergyError("La alergia no fue encontrada o ya está inactiva.");