using Application.Core.DTOs.Prescription.Errors;
using Application.Core.DTOs.Prescription.Request;
using Application.Core.DTOs.Prescription.Response;
using BindSharp;

namespace Application.Core.Interfaces.Prescription;

public interface IPrescription
{
    /// <summary>Returns all Borrador prescriptions created by the given doctor.</summary>
    Task<Result<IReadOnlyList<DoctorDraftPrescriptionResponse>, PrescriptionError>> GetDoctorDraftPrescriptionsAsync(
        Guid doctorUserCode);

    /// <summary>Returns all prescriptions dispensed by the given nurse on the specified date.</summary>
    Task<Result<IReadOnlyList<NurseDispensationResponse>, PrescriptionError>> GetNurseDispensationsByDateAsync(
        Guid nurseUserCode, DateOnly date);

    /// <summary>Creates a new prescription (initial status: Borrador).</summary>
    Task<Result<PrescriptionResponse, PrescriptionError>> CreateAsync(
        CreatePrescriptionRequest request, Guid createdByUserCode);

    /// <summary>Returns the full prescription with its detail lines.</summary>
    Task<Result<PrescriptionResponse, PrescriptionError>> GetAsync(Guid code);

    /// <summary>Returns the prescription only if it belongs to the given patient. Returns Forbidden otherwise.</summary>
    Task<Result<PrescriptionResponse, PrescriptionError>> GetForPatientAsync(Guid code, Guid patientCode);

    /// <summary>Updates notes, validUntil and lines. Only allowed when status is Borrador.</summary>
    Task<Result<PrescriptionResponse, PrescriptionError>> UpdateAsync(
        Guid code, UpdatePrescriptionRequest request, Guid modifiedByUserCode);

    /// <summary>Transitions Borrador → Activo.</summary>
    Task<Result<Unit, PrescriptionError>> SignAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Activo → Suspendido.</summary>
    Task<Result<Unit, PrescriptionError>> SuspendAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Suspendido → Activo.</summary>
    Task<Result<Unit, PrescriptionError>> ReactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions any non-terminal status → Cancelado.</summary>
    Task<Result<Unit, PrescriptionError>> CancelAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Activo → Dispensado.</summary>
    Task<Result<Unit, PrescriptionError>> DispenseAsync(Guid code, Guid performedByUserCode);
}