namespace Infrastructure.Core.DTOs.Mfa;

public abstract record MfaRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetMfaError(string? Details = null, Exception? Exception = null)
    : MfaRepositoryError("Error inesperado al recuperar la configuración MFA.", Details, Exception);

public sealed record UpsertPendingMfaError(string? Details = null, Exception? Exception = null)
    : MfaRepositoryError("Error inesperado al iniciar el registro MFA.", Details, Exception);

public sealed record ConfirmMfaError(string? Details = null, Exception? Exception = null)
    : MfaRepositoryError("Error inesperado al confirmar el registro MFA.", Details, Exception);

public sealed record AdvanceMfaStepError(string? Details = null, Exception? Exception = null)
    : MfaRepositoryError("Error inesperado al registrar el código MFA usado.", Details, Exception);

public sealed record DeleteMfaError(string? Details = null, Exception? Exception = null)
    : MfaRepositoryError("Error inesperado al restablecer el MFA.", Details, Exception);
