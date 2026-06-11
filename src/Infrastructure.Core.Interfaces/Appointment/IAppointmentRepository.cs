using BindSharp;
using Infrastructure.Core.DTOs.Appointment;
using Infrastructure.Core.Models.Appointment;

namespace Infrastructure.Core.Interfaces.Appointment;

public interface IAppointmentRepository
{
    /// <summary>
    /// Atomically locks the availability slot and inserts the appointment.
    /// Returns null when the slot is already booked (race condition → 409).
    /// Returns a typed error for data-access failures or validation mismatches.
    /// </summary>
    Task<Result<AppointmentRow?, AppointmentRepositoryError>> InsertAsync(
        Guid patientCode,
        Guid availabilityCode,
        Guid consultationTypeCode);

    /// <summary>Returns the appointment row, or null if not found.</summary>
    Task<Result<AppointmentRow?, AppointmentRepositoryError>> GetByCodeAsync(Guid code);

    /// <summary>Transitions PendientePago → Confirmado for the owning patient. Returns rows affected.</summary>
    Task<Result<int, AppointmentRepositoryError>> ConfirmPaymentAsync(Guid code, Guid patientCode);

    /// <summary>
    /// Transitions PendientePago/Confirmado → Cancelado and releases the slot atomically.
    /// When <paramref name="patientCode"/> is non-null, ownership is enforced in SQL.
    /// </summary>
    Task<Result<int, AppointmentRepositoryError>> CancelAsync(Guid code, Guid? patientCode);

    /// <summary>Transitions Confirmado → Completado. Returns rows affected.</summary>
    Task<Result<int, AppointmentRepositoryError>> CompleteAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Confirmado → NoAsistio. Returns rows affected.</summary>
    Task<Result<int, AppointmentRepositoryError>> NoShowAsync(Guid code, Guid performedByUserCode);

    /// <summary>Returns a page of appointments for the given patient, ordered by ScheduledAt DESC.</summary>
    Task<Result<(IEnumerable<AppointmentRow> Items, int Total), AppointmentRepositoryError>>
        GetPatientAppointmentsAsync(
            Guid patientCode, int page, int pageSize);
}