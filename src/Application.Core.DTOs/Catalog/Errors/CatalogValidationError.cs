namespace Application.Core.DTOs.Catalog.Errors;

/// <summary>Which catalog item a validation failure is about; used for the user-facing message.</summary>
public enum CatalogItemKind
{
    Medication,
    AdministrationRoute,
    Frequency,
    Specialty,
    Insurance,
    Sex,
    DocumentType,
    Allergy,
    AllergySeverity
}

/// <summary>Why a catalog-backed write was rejected. Replaces the DB foreign keys that no longer span databases.</summary>
public abstract record CatalogValidationError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>The code does not exist in the catalog.</summary>
public sealed record CatalogItemNotFoundError(CatalogItemKind Kind)
    : CatalogValidationError($"{CatalogItemLabels.For(Kind)} no existe en el catálogo.");

/// <summary>The code exists but has been deactivated (rejected for new use; still resolves for history).</summary>
public sealed record CatalogItemInactiveError(CatalogItemKind Kind)
    : CatalogValidationError($"{CatalogItemLabels.For(Kind)} está {CatalogItemLabels.Agree(Kind, "desactivad")} en el catálogo.");

/// <summary>The catalog item is active, but this partner has not enabled it.</summary>
public sealed record CatalogItemNotEnabledError(CatalogItemKind Kind)
    : CatalogValidationError($"{CatalogItemLabels.For(Kind)} no está {CatalogItemLabels.Agree(Kind, "habilitad")} en este centro.");

/// <summary>No catalog snapshot has loaded (cold start without catalog access), so nothing can be validated.</summary>
public sealed record CatalogValidationUnavailableError()
    : CatalogValidationError("El catálogo compartido no está disponible en este momento. Intente nuevamente más tarde.");

/// <summary>Reading the partner's enabled flags failed.</summary>
public sealed record CatalogValidationDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : CatalogValidationError(Message, Details, Exception);

internal static class CatalogItemLabels
{
    /// <summary>Appends the participle ending ("o"/"a") matching the grammatical gender of the item's label.</summary>
    internal static string Agree(CatalogItemKind kind, string stem) =>
        stem + (For(kind).StartsWith("La ", StringComparison.Ordinal) ? "a" : "o");

    internal static string For(CatalogItemKind kind) => kind switch
    {
        CatalogItemKind.Medication => "El medicamento",
        CatalogItemKind.AdministrationRoute => "La vía de administración",
        CatalogItemKind.Frequency => "La frecuencia",
        CatalogItemKind.Specialty => "La especialidad",
        CatalogItemKind.Insurance => "El seguro",
        CatalogItemKind.Sex => "El sexo",
        CatalogItemKind.DocumentType => "El tipo de documento",
        CatalogItemKind.Allergy => "La alergia",
        CatalogItemKind.AllergySeverity => "La severidad de la alergia",
        _ => "El elemento"
    };
}
