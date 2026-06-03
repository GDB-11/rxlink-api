namespace Application.Core.DTOs.Role.Errors;

public abstract record RoleError(string Message, string? Details = null, Exception? Exception = null);

public sealed record RoleDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : RoleError(Message, Details, Exception);

public sealed record RoleNotFoundError()
    : RoleError("El rol no fue encontrado o ya está inactivo.");

public sealed record RoleDuplicateNameError()
    : RoleError("Ya existe un rol activo con ese nombre.");