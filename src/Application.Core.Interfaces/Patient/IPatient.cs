using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Patient.Request;
using Application.Core.DTOs.Patient.Response;
using BindSharp;

namespace Application.Core.Interfaces.Patient;

public interface IPatient
{
    /// <summary>Adds an allergy to an existing patient.</summary>
    Task<Result<PatientAllergyResponse, PatientError>> AddAllergyAsync(
        Guid patientCode, PatientAllergyRequest request);

    /// <summary>Updates the severity and notes of an existing patient allergy.</summary>
    Task<Result<PatientAllergyResponse, PatientError>> UpdateAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, PatientAllergyRequest request);

    /// <summary>Soft-deletes a patient allergy.</summary>
    Task<Result<Unit, PatientError>> RemoveAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, Guid performedByUserCode);


    /// <summary>Returns a paginated list of patients. Supports optional text search on names or surnames.</summary>
    Task<Result<PatientPageResponse, PatientError>> GetPageAsync(PatientPageRequest request);

    /// <summary>Registers a new patient.</summary>
    Task<Result<PatientResponse, PatientError>> CreateAsync(CreatePatientRequest request);

    /// <summary>Updates an existing active patient identified by its code.</summary>
    Task<Result<PatientResponse, PatientError>> UpdateAsync(Guid code, UpdatePatientRequest request);

    /// <summary>Deactivates a patient (soft-delete). The record is preserved to maintain FK integrity.</summary>
    Task<Result<Unit, PatientError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Reactivates a previously deactivated patient.</summary>
    Task<Result<Unit, PatientError>> ActivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Returns the patient's own profile (including allergies) derived from the JWT patient_code.</summary>
    Task<Result<PatientResponse, PatientError>> GetSelfAsync(Guid patientCode);

    /// <summary>Returns the patient profile linked to the given person code.</summary>
    Task<Result<PatientResponse, PatientError>> GetByPersonCodeAsync(Guid personCode);

    /// <summary>Updates the patient's own contact fields (phone, alternativePhone, address, emergency contact).</summary>
    Task<Result<Unit, PatientError>> UpdateSelfAsync(Guid patientCode, UpdatePatientSelfRequest request);
}