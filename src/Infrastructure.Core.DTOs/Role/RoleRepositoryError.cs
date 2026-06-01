namespace Infrastructure.Core.DTOs.Role;

public abstract record RoleRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetRolePageError(string? Details = null, Exception? Exception = null)
    : RoleRepositoryError("Error inesperado al recuperar los roles.", Details, Exception);

public sealed record InsertRoleError(string? Details = null, Exception? Exception = null)
    : RoleRepositoryError("Error inesperado al registrar el rol.", Details, Exception);

public sealed record UpdateRoleError(string? Details = null, Exception? Exception = null)
    : RoleRepositoryError("Error inesperado al actualizar el rol.", Details, Exception);

public sealed record DeactivateRoleError(string? Details = null, Exception? Exception = null)
    : RoleRepositoryError("Error inesperado al desactivar el rol.", Details, Exception);

public sealed record ActivateRoleError(string? Details = null, Exception? Exception = null)
    : RoleRepositoryError("Error inesperado al activar el rol.", Details, Exception);