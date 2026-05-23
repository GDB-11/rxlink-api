namespace Infrastructure.Core.DTOs.Specialty;
public abstract record SpecialtyRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetSpecialtyPageError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al recuperar los especialidad.", Details, Exception);

public sealed record InsertSpecialtyError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al registrar el especialidad.", Details, Exception);

public sealed record UpdateSpecialtyError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al actualizar el especialidad.", Details, Exception);

public sealed record DeactivateSpecialtyError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al desactivar el especialidad.", Details, Exception);