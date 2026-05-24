namespace Application.Core.DTOs.User.Errors;

public abstract record UserError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record UserDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : UserError(Message, Details, Exception);

/// <summary>The target user does not exist or has already been deleted.</summary>
public sealed record UserNotFoundError()
    : UserError("El usuario no fue encontrado.");

/// <summary>The provided role does not exist, is inactive, or a unique constraint was violated on create.</summary>
public sealed record UserRoleNotFoundError()
    : UserError("El rol proporcionado no existe o está inactivo.");

/// <summary>An error occurred while hashing the user's password.</summary>
public sealed record UserPasswordError()
    : UserError("Error al procesar la contraseña del usuario.");
