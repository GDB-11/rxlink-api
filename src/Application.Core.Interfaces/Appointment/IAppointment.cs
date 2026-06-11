using Application.Core.DTOs.Appointment.Errors;
using Application.Core.DTOs.Appointment.Request;
using Application.Core.DTOs.Appointment.Response;
using BindSharp;

namespace Application.Core.Interfaces.Appointment;

public interface IAppointment
{
    /// <summary>Creates a new appointment, locking the availability slot atomically.</summary>
    Task<Result<AppointmentResponse, AppointmentError>> CreateAsync(
        CreateAppointmentRequest request, Guid patientCode);

    /// <summary>Transitions PendientePago → Confirmado. Patient must own the appointment.</summary>
    Task<Result<Unit, AppointmentError>> ConfirmPaymentAsync(Guid code, Guid patientCode);

    /// <summary>
    /// Transitions PendientePago/Confirmado → Cancelado and releases the slot.
    /// <paramref name="callerCode"/> is the patient_code when <paramref name="callerRole"/> is "Patient",
    /// or the user_code (Guid) when "Administrador".
    /// </summary>
    Task<Result<Unit, AppointmentError>> CancelAsync(Guid code, Guid callerCode, string callerRole);

    /// <summary>Transitions Confirmado → Completado. The caller must be the assigned doctor or admin.</summary>
    Task<Result<Unit, AppointmentError>> CompleteAsync(Guid code, Guid callerUserCode, string callerRole);

    /// <summary>Transitions Confirmado → NoAsistio. Admin only.</summary>
    Task<Result<Unit, AppointmentError>> NoShowAsync(Guid code, Guid adminUserCode);

    /// <summary>
    /// Returns the appointment detail.
    /// Authorization: patient must own it; doctor must be assigned; admin can see any.
    /// <paramref name="callerCode"/> is patient_code for "Patient", user_code for others.
    /// </summary>
    Task<Result<AppointmentResponse, AppointmentError>> GetAsync(Guid code, Guid callerCode, string callerRole);

    /// <summary>Returns the authenticated patient's appointments, ordered by scheduledAt DESC.</summary>
    Task<Result<AppointmentPageResponse, AppointmentError>> GetPatientAppointmentsAsync(
        Guid patientCode, AppointmentPageRequest request);
}