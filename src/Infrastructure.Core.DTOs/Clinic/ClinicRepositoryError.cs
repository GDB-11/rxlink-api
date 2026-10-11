namespace Infrastructure.Core.DTOs.Clinic;

public abstract record ClinicRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetClinicError(string? Details = null, Exception? Exception = null)
    : ClinicRepositoryError("Error inesperado al recuperar el centro de atención.", Details, Exception);

public sealed record UpdateClinicError(string? Details = null, Exception? Exception = null)
    : ClinicRepositoryError("Error inesperado al actualizar el centro de atención.", Details, Exception);
