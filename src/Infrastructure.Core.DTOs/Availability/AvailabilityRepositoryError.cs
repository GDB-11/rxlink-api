namespace Infrastructure.Core.DTOs.Availability;

public abstract record AvailabilityRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetDoctorUserIdError(string? Details = null, Exception? Exception = null)
    : AvailabilityRepositoryError("Error inesperado al validar el doctor.", Details, Exception);

public sealed record InsertAvailabilityError(string? Details = null, Exception? Exception = null)
    : AvailabilityRepositoryError("Error inesperado al registrar el slot de disponibilidad.", Details, Exception);

public sealed record GetAvailabilityError(string? Details = null, Exception? Exception = null)
    : AvailabilityRepositoryError("Error inesperado al recuperar los slots de disponibilidad.", Details, Exception);

public sealed record GetSlotForDeletionError(string? Details = null, Exception? Exception = null)
    : AvailabilityRepositoryError("Error inesperado al verificar el estado del slot.", Details, Exception);

public sealed record SoftDeleteAvailabilityError(string? Details = null, Exception? Exception = null)
    : AvailabilityRepositoryError("Error inesperado al eliminar el slot de disponibilidad.", Details, Exception);

public sealed record GetAvailableDatesError(string? Details = null, Exception? Exception = null)
    : AvailabilityRepositoryError("Error inesperado al recuperar las fechas disponibles.", Details, Exception);

public sealed record GetAvailableSlotsError(string? Details = null, Exception? Exception = null)
    : AvailabilityRepositoryError("Error inesperado al recuperar los horarios disponibles.", Details, Exception);