namespace Infrastructure.Core.DTOs.Specialty;
public abstract record SpecialtyRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetSpecialtyPageError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al recuperar las especialidades.", Details, Exception);

public sealed record InsertSpecialtyError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al registrar la especialidad.", Details, Exception);

public sealed record UpdateSpecialtyError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al actualizar la especialidad.", Details, Exception);

public sealed record DeactivateSpecialtyError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al desactivar la especialidad.", Details, Exception);

public sealed record GetActiveSpecialtiesWithCountError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al recuperar el listado de especialidades.", Details, Exception);

public sealed record GetDoctorsBySpecialtyError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al recuperar los médicos de la especialidad.", Details, Exception);