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