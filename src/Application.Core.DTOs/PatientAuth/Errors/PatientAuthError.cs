namespace Application.Core.DTOs.PatientAuth.Errors;

public abstract record PatientAuthError(string Message, string? Details = null, Exception? Exception = null);

public sealed record PatientNotFoundError()
    : PatientAuthError("No se encontró una cuenta con ese correo electrónico.");

public sealed record PatientInactiveError()
    : PatientAuthError("La cuenta se encuentra inactiva. Contacte al administrador.");

public sealed record PatientNoCredentialsError()
    : PatientAuthError("Esta cuenta no tiene acceso al app. Contacte al administrador.");

public sealed record PatientIncorrectPasswordError()
    : PatientAuthError("Correo electrónico o contraseña incorrectos.");

public sealed record PatientAlreadyRegisteredError()
    : PatientAuthError("Ya existe una cuenta con esas credenciales. Por favor inicia sesión.");

public sealed record PersonNotFoundError()
    : PatientAuthError("La persona indicada no existe en el sistema.");

public sealed record PatientRefreshTokenNotFoundError()
    : PatientAuthError("Sesión no encontrada o expirada.");

public sealed record PatientJwtGenerationError(string? Details = null, Exception? Exception = null)
    : PatientAuthError("Error al generar el token de acceso.", Details, Exception);

public sealed record PatientJwtStorageError(string? Details = null, Exception? Exception = null)
    : PatientAuthError("Error al almacenar la sesión.", Details, Exception);

public sealed record PatientPasswordHashError(string Message, string? Details = null, Exception? Exception = null)
    : PatientAuthError(Message, Details, Exception);

public sealed record PatientRepositoryError(string Message, string? Details = null, Exception? Exception = null)
    : PatientAuthError(Message, Details, Exception);