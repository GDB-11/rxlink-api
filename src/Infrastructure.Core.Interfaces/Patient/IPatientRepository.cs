using BindSharp;
using Infrastructure.Core.DTOs.Patient;
using Infrastructure.Core.Models.Patient;

namespace Infrastructure.Core.Interfaces.Patient;

public interface IPatientRepository
{
    /// <summary>Returns one page of patients, with a total count via window function.</summary>
    Task<Result<IEnumerable<PatientRow>, PatientRepositoryError>> GetPageAsync(int offset, int limit, string? search);

    /// <summary>
    /// Inserts a Patient linked to an existing Person identified by <paramref name="personCode"/>.
    /// Auto-generates the MedicalRecordNumber (PAC-YYYYMM-NNNNN format).
    /// <paramref name="allergiesJson"/> is a JSON array of <c>{ AllergyCode, Notes }</c> objects.
    /// Returns <c>null</c> when PersonCode does not match any registered person.
    /// </summary>
    Task<Result<PatientRow?, PatientRepositoryError>> InsertAsync(
        Guid personCode, string allergiesJson);

    /// <summary>
    /// Updates the MedicalRecordNumber of an active patient. Person data is immutable through this endpoint.
    /// Returns <c>null</c> when no matching active row exists.
    /// </summary>
    Task<Result<PatientRow?, PatientRepositoryError>> UpdateAsync(
        Guid code, string medicalRecordNumber);

    /// <summary>Soft-deletes an active patient. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, PatientRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Reactivates a previously deactivated patient. Returns the number of affected rows.</summary>
    Task<Result<int, PatientRepositoryError>> ActivateAsync(Guid code);
}
