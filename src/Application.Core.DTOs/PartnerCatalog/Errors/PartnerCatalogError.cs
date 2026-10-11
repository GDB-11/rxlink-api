using Application.Core.DTOs.Catalog.Errors;

namespace Application.Core.DTOs.PartnerCatalog.Errors;

public abstract record PartnerCatalogError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record PartnerCatalogDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : PartnerCatalogError(Message, Details, Exception);

/// <summary>The catalog code does not exist, or is deactivated and the caller tried to enable it.</summary>
public sealed record PartnerCatalogItemRejectedError(CatalogValidationError Reason)
    : PartnerCatalogError(Reason.Message);

/// <summary>The resulting setting breaks a business rule (e.g. enabling a specialty without prices).</summary>
public sealed record PartnerCatalogRuleError(string Message) : PartnerCatalogError(Message);
