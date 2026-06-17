namespace Application.Core.DTOs.Appointment.Errors;

public abstract record AppointmentError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record AppointmentDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : AppointmentError(Message, Details, Exception);

/// <summary>The appointment was not found.</summary>
public sealed record AppointmentNotFoundError()
    : AppointmentError("La cita no fue encontrada.");

/// <summary>The patient was not found or is inactive.</summary>
public sealed record AppointmentPatientNotFoundError()
    : AppointmentError("El paciente no fue encontrado.");

/// <summary>The availability slot was not found or was deleted.</summary>
public sealed record AppointmentSlotNotFoundError()
    : AppointmentError("El horario de disponibilidad no fue encontrado.");

/// <summary>The availability slot is already booked or a race condition occurred.</summary>
public sealed record AppointmentSlotAlreadyBookedError()
    : AppointmentError("El horario ya no está disponible.");

/// <summary>The availability slot date is in the past.</summary>
public sealed record AppointmentSlotExpiredError()
    : AppointmentError("El horario seleccionado ya pasó.");

/// <summary>The consultation type was not found or is inactive.</summary>
public sealed record AppointmentConsultationTypeNotFoundError()
    : AppointmentError("El tipo de consulta no fue encontrado.");

/// <summary>The requested state transition is not valid for the current appointment status.</summary>
public sealed record AppointmentInvalidTransitionError()
    : AppointmentError("La transición de estado solicitada no es válida para el estado actual de la cita.");

/// <summary>The caller does not have permission to perform this action on the appointment.</summary>
public sealed record AppointmentForbiddenError()
    : AppointmentError("No tiene permiso para realizar esta acción sobre la cita.");

/// <summary>The appointment is not in PendientePago status.</summary>
public sealed record AdminConfirmPaymentConflictError()
    : AppointmentError("La cita no está en estado PendientePago.");

/// <summary>The appointment is not in Confirmado status.</summary>
public sealed record RevertPaymentConflictError()
    : AppointmentError("La cita no está en estado Confirmado.");