using BindSharp;
using Infrastructure.Core.DTOs.Prescription;
using Infrastructure.Core.Models.Prescription;

namespace Infrastructure.Core.Interfaces.Prescription;

public interface IPrescriptionRepository
{
    /// <summary>
    /// Returns all non-deleted prescriptions in Borrador status created by the given doctor.
    /// Ordered by CreatedAt DESC.
    /// </summary>
    Task<Result<IReadOnlyList<DoctorDraftPrescriptionRow>, PrescriptionRepositoryError>>
        GetDoctorDraftPrescriptionsAsync(Guid doctorUserCode);

    /// <summary>
    /// Returns all prescriptions dispensed by the given nurse on the specified date.
    /// Ordered by DispensedAt DESC.
    /// </summary>
    Task<Result<IReadOnlyList<NurseDispensationRow>, PrescriptionRepositoryError>> GetNurseDispensationsByDateAsync(
        Guid nurseUserCode, DateTime date);

    /// <summary>
    /// Inserts a new prescription (status: Borrador) with its detail lines.
    /// Returns null when the diagnostic is not found or inactive.
    /// Returns <see cref="InsertPrescriptionDuplicateError"/> when a non-deleted prescription already exists for the diagnostic.
    /// </summary>
    Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> InsertAsync(
        Guid diagnosticCode, string? notes, DateTime validUntil, string detailsJson, Guid createdByUserCode);

    /// <summary>
    /// Returns the full prescription with detail lines.
    /// Returns null when not found or deleted.
    /// </summary>
    Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> GetByCodeAsync(Guid code);

    /// <summary>
    /// Updates notes, validUntil and lines. Only succeeds when status is Borrador.
    /// Returns null when not found or deleted.
    /// Returns <see cref="UpdatePrescriptionInvalidStatusError"/> when status is not Borrador.
    /// </summary>
    Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> UpdateAsync(
        Guid code, string? notes, DateTime validUntil, string detailsJson, Guid modifiedByUserCode);

    /// <summary>
    /// Returns the medications of the patient's <c>Activo</c> prescriptions, excluding <paramref name="excludePrescriptionCode"/>
    /// (the one being checked).
    /// </summary>
    Task<Result<IReadOnlyList<ActivePatientMedicationRow>, PrescriptionRepositoryError>>
        GetActiveMedicationsOfPatientAsync(Guid patientCode, Guid excludePrescriptionCode);

    /// <summary>Transitions Borrador → Activo. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> SignAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Activo → Suspendido. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> SuspendAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Suspendido → Activo. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> ReactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions any non-terminal status → Cancelado. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> CancelAsync(Guid code, Guid performedByUserCode);

    /// <summary>
    /// Transitions Activo → Dispensado and, in the same transaction, writes one <c>Dispense</c> stock movement per
    /// line and decrements the default clinic's <c>ClinicStock</c> (rows locked FOR UPDATE).
    /// Returns 0 when the transition is invalid; fails with <see cref="DispenseLinesInvalidError"/> or
    /// <see cref="InsufficientStockError"/> (nothing is written).
    /// With an <paramref name="overrideReason"/> a balance that is too low no longer fails: the shortfall is
    /// recorded on the movement, the balance goes negative and a <c>Stock.OverrideDispense</c> event is written.
    /// </summary>
    Task<Result<int, PrescriptionRepositoryError>> DispenseAsync(
        Guid code, Guid performedByUserCode, IReadOnlyList<DispenseLineRow> lines, string? overrideReason);
}