namespace Application.Core.DTOs.Lookup.Errors;

public abstract record LookupError(string Message, string? Details = null, Exception? Exception = null);

public sealed record LookupDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : LookupError(Message, Details, Exception);

/// <summary>No catalog snapshot has loaded (cold start without catalog access), so the list cannot be served.</summary>
public sealed record LookupCatalogUnavailableError()
    : LookupError("El catálogo compartido no está disponible en este momento. Intente nuevamente más tarde.");
