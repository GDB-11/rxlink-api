namespace Infrastructure.Core.DTOs.Catalog;

public abstract record CatalogRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record CatalogLoadError(string? Details = null, Exception? Exception = null)
    : CatalogRepositoryError("No se pudo leer el catálogo compartido.", Details, Exception);

/// <summary>No snapshot has ever loaded (cold start without catalog access): catalog-dependent writes can't be validated.</summary>
public sealed record CatalogUnavailableError()
    : CatalogRepositoryError("El catálogo compartido no está disponible en este momento. Intente nuevamente más tarde.");


/// <summary>The partner's enabled-items tables (PartnerMedication/Specialty/Insurance) could not be read.</summary>
public sealed record PartnerEnablementReadError(string? Details = null, Exception? Exception = null)
    : CatalogRepositoryError("No se pudo leer la configuración del centro.", Details, Exception);
