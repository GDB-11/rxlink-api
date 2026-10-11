namespace Application.Core.DTOs.Person.Errors;

public abstract record PersonError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record PersonDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : PersonError(Message, Details, Exception);

/// <summary>The target person does not exist.</summary>
public sealed record PersonNotFoundError()
    : PersonError("La persona no fue encontrada.");

/// <summary>A catalog code in the request was rejected (missing, deactivated, not enabled) or the catalog is unavailable.</summary>
public sealed record PersonCatalogValidationError(Application.Core.DTOs.Catalog.Errors.CatalogValidationError Inner)
    : PersonError(Inner.Message, Inner.Details, Inner.Exception);
