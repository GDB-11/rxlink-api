namespace Infrastructure.Core.DTOs.Allergy;

public abstract record AllergyRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetAllergiesPageError(string? Details = null, Exception? Exception = null)
    : AllergyRepositoryError("Error inesperado al recuperar las alergias.", Details, Exception);

public sealed record InsertAllergyError(string? Details = null, Exception? Exception = null)
    : AllergyRepositoryError("Error inesperado al registrar la alergia.", Details, Exception);

public sealed record UpdateAllergyError(string? Details = null, Exception? Exception = null)
    : AllergyRepositoryError("Error inesperado al actualizar la alergia.", Details, Exception);

public sealed record DeactivateAllergyError(string? Details = null, Exception? Exception = null)
    : AllergyRepositoryError("Error inesperado al desactivar la alergia.", Details, Exception);
