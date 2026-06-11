namespace Application.Core.DTOs.Person.Errors;

public abstract record PersonError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record PersonDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : PersonError(Message, Details, Exception);

/// <summary>The target person does not exist.</summary>
public sealed record PersonNotFoundError()
    : PersonError("La persona no fue encontrada.");