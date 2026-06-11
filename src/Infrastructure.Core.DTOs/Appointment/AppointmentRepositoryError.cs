namespace Infrastructure.Core.DTOs.Appointment;

public abstract record AppointmentRepositoryError(string Message, string? Details = null, Exception? Exception = null);

// Insert-specific validation errors (not DB failures)
public sealed record InsertPatientNotFoundError()
    : AppointmentRepositoryError("Paciente no encontrado.");

public sealed record InsertSlotNotFoundError()
    : AppointmentRepositoryError("El slot no fue encontrado o fue eliminado.");

public sealed record InsertSlotAlreadyBookedError()
    : AppointmentRepositoryError("El slot ya está reservado.");

public sealed record InsertSlotExpiredError()
    : AppointmentRepositoryError("El slot pertenece a una fecha pasada.");

public sealed record InsertConsultationTypeNotFoundError()
    : AppointmentRepositoryError("El tipo de consulta no fue encontrado.");

// DB-level errors
public sealed record InsertAppointmentError(string? Details = null, Exception? Exception = null)
    : AppointmentRepositoryError("Error inesperado al registrar la cita.", Details, Exception);

public sealed record GetAppointmentError(string? Details = null, Exception? Exception = null)
    : AppointmentRepositoryError("Error inesperado al recuperar la cita.", Details, Exception);

public sealed record TransitionAppointmentError(string? Details = null, Exception? Exception = null)
    : AppointmentRepositoryError("Error inesperado al actualizar el estado de la cita.", Details, Exception);

public sealed record GetPatientAppointmentsError(string? Details = null, Exception? Exception = null)
    : AppointmentRepositoryError("Error inesperado al recuperar las citas del paciente.", Details, Exception);