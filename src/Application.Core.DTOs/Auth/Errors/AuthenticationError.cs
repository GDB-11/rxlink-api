namespace Application.Core.DTOs.Auth.Errors;

public abstract record AuthenticationError(string Message, string? Details = null, Exception? Exception = null);

public sealed record StoreRefreshTokenError(string Message, string? Details, Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);

public sealed record GetByUsernameAsyncDomainError(string Message, string? Details = null, Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);

public sealed record GetByRefreshTokenAsyncDomainError(
    string Message,
    string? Details = null,
    Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);

public sealed record UserNotFoundError()
    : AuthenticationError("El usuario no existe.");

public sealed record IncorrectPasswordError()
    : AuthenticationError("La contraseña es incorrecta.");

public sealed record RefreshTokenNotFoundError()
    : AuthenticationError("Sesión no encontrada o expirada.");

public sealed record JwtGenerationError(string? Details = null, Exception? Exception = null)
    : AuthenticationError("Error al generar el token de acceso.", Details, Exception);

public sealed record UserInactiveError()
    : AuthenticationError("El usuario se encuentra inactivo. Contacte al administrador.");

public sealed record InvalidUserTokenError()
    : AuthenticationError("Usuario o contraseña incorrectos.");

public sealed record ChaChaDecryptError(string Message, string? Details, Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);

public sealed record JwtStorageError(string? Details, Exception? Exception = null)
    : AuthenticationError("Error al almacenar la sesión.", Details, Exception);

/// <summary>Password was correct but the account has MFA: the client must call <c>mfa/verify</c> with this token.</summary>
public sealed record MfaRequiredError(string MfaToken, DateTime ExpiresAt)
    : AuthenticationError("Se requiere un código de verificación (MFA).");

public sealed record InvalidMfaTokenError()
    : AuthenticationError("La verificación MFA expiró. Inicie sesión nuevamente.");

public sealed record InvalidMfaCodeError()
    : AuthenticationError("El código de verificación es incorrecto o ya fue utilizado.");

public sealed record MfaVerificationError(string Message, string? Details = null, Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);

/// <summary>Too many consecutive failed attempts: credential checks are refused until <paramref name="LockedUntil"/>.</summary>
public sealed record AccountLockedError(DateTime LockedUntil, int RetryAfterSeconds)
    : AuthenticationError(
        $"Cuenta bloqueada temporalmente por intentos fallidos. Intente nuevamente en {Math.Max(1, (int)Math.Ceiling(RetryAfterSeconds / 60.0))} minuto(s).");

/// <summary>The login session reached its absolute time limit and cannot be refreshed any more.</summary>
public sealed record SessionExpiredError()
    : AuthenticationError("La sesión superó el tiempo máximo permitido. Inicie sesión nuevamente.");
