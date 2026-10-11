namespace Infrastructure.Core.DTOs.Specialty;

public abstract record SpecialtyRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetActiveSpecialtiesWithCountError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al recuperar el listado de especialidades.", Details, Exception);

public sealed record GetDoctorsBySpecialtyError(string? Details = null, Exception? Exception = null)
    : SpecialtyRepositoryError("Error inesperado al recuperar los médicos de la especialidad.", Details, Exception);