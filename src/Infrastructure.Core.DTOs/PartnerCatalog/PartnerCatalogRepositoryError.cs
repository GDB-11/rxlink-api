namespace Infrastructure.Core.DTOs.PartnerCatalog;

public abstract record PartnerCatalogRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetPartnerCatalogSettingError(string? Details = null, Exception? Exception = null)
    : PartnerCatalogRepositoryError("Error inesperado al recuperar la configuración del centro.", Details, Exception);

public sealed record SavePartnerCatalogSettingError(string? Details = null, Exception? Exception = null)
    : PartnerCatalogRepositoryError("Error inesperado al guardar la configuración del centro.", Details, Exception);
