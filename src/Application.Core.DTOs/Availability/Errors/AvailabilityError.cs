namespace Application.Core.DTOs.Availability.Errors;

public abstract record AvailabilityError(string Message, string? Details = null, Exception? Exception = null);

public sealed record AvailabilityDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : AvailabilityError(Message, Details, Exception);

public sealed record AvailabilityDoctorNotFoundError()
    : AvailabilityError("El doctor no fue encontrado o no está activo.");

public sealed record AvailabilityNotFoundError()
    : AvailabilityError("El slot de disponibilidad no fue encontrado o ya fue eliminado.");

public sealed record AvailabilityAlreadyBookedError()
    : AvailabilityError("El slot ya está reservado y no puede ser eliminado.");

public sealed record AvailabilityInvalidTimeFormatError(string Value)
    : AvailabilityError($"El formato de hora '{Value}' no es válido. Use HH:MM.");