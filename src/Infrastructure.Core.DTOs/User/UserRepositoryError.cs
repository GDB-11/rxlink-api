namespace Infrastructure.Core.DTOs.User;

public abstract record UserRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetUsersPageError(string? Details = null, Exception? Exception = null)
    : UserRepositoryError("Error inesperado al recuperar los usuarios.", Details, Exception);

public sealed record InsertUserError(string? Details = null, Exception? Exception = null)
    : UserRepositoryError("Error inesperado al registrar el usuario.", Details, Exception);

public sealed record UpdateUserError(string? Details = null, Exception? Exception = null)
    : UserRepositoryError("Error inesperado al actualizar el usuario.", Details, Exception);

public sealed record DeactivateUserError(string? Details = null, Exception? Exception = null)
    : UserRepositoryError("Error inesperado al desactivar el usuario.", Details, Exception);
    
public sealed record ActivateUserError(string? Details = null, Exception? Exception = null)
    : UserRepositoryError("Error inesperado al activar el usuario.", Details, Exception);    