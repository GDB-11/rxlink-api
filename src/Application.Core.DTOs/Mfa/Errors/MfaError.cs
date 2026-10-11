namespace Application.Core.DTOs.Mfa.Errors;

public abstract record MfaError(string Message, string? Details = null, Exception? Exception = null);

public sealed record MfaDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : MfaError(Message, Details, Exception);

public sealed record MfaSecretProtectionError(string Message, string? Details = null, Exception? Exception = null)
    : MfaError(Message, Details, Exception);

public sealed record MfaAlreadyEnabledError()
    : MfaError("El MFA ya está activado. Un administrador debe restablecerlo antes de volver a registrarlo.");

public sealed record MfaNotPendingError()
    : MfaError("No hay un registro MFA pendiente. Inicie el registro nuevamente.");

public sealed record MfaNotEnabledError()
    : MfaError("El usuario no tiene MFA activado.");

public sealed record MfaInvalidCodeError()
    : MfaError("El código de verificación es incorrecto o ya fue utilizado.");
