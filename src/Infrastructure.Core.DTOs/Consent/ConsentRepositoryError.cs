namespace Infrastructure.Core.DTOs.Consent;

public abstract record ConsentRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record InsertConsentError(string? Details = null, Exception? Exception = null)
    : ConsentRepositoryError("Error inesperado al registrar el consentimiento.", Details, Exception);

public sealed record RevokeConsentError(string? Details = null, Exception? Exception = null)
    : ConsentRepositoryError("Error inesperado al revocar el consentimiento.", Details, Exception);

public sealed record GetConsentError(string? Details = null, Exception? Exception = null)
    : ConsentRepositoryError("Error inesperado al recuperar los consentimientos.", Details, Exception);
