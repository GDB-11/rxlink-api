namespace Application.Core.DTOs.Catalog.Errors;

/// <summary>Why a catalog read (snapshot merged with the partner's own settings) failed.</summary>
public abstract record CatalogReadError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>No catalog snapshot has loaded (cold start without catalog access), so there is nothing to list.</summary>
public sealed record CatalogReadUnavailableError()
    : CatalogReadError("El catálogo compartido no está disponible en este momento. Intente nuevamente más tarde.");

/// <summary>Reading the partner's own settings failed.</summary>
public sealed record CatalogReadDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : CatalogReadError(Message, Details, Exception);
