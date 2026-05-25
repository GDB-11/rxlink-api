using BindSharp;
using Infrastructure.Core.DTOs.Patient;
using Infrastructure.Core.Models.Patient;

namespace Infrastructure.Core.Interfaces.Patient;

public interface IPatientRepository
{
    /// <summary>Returns one page of patients, with a total count via window function.</summary>
    Task<Result<IEnumerable<PatientRow>, PatientRepositoryError>> GetPageAsync(int offset, int limit, string? search);

    /// <summary>Inserts a new patient and returns the created row, or <c>null</c> on unexpected failure.</summary>
    Task<Result<PatientRow?, PatientRepositoryError>> InsertAsync(
        string names, string surnames, DateOnly birthDate, string phone,
        string? alternativePhone, string email, string? address,
        string? emergencyContactName, string? emergencyContactPhone,
        string medicalRecordNumber);

    /// <summary>Updates an active patient by code. Returns <c>null</c> when no matching active row exists.</summary>
    Task<Result<PatientRow?, PatientRepositoryError>> UpdateAsync(
        Guid code, string names, string surnames, DateOnly birthDate, string phone,
        string? alternativePhone, string email, string? address,
        string? emergencyContactName, string? emergencyContactPhone,
        string medicalRecordNumber);

    /// <summary>Soft-deletes an active patient. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, PatientRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Reactivates a previously deactivated patient. Returns the number of affected rows.</summary>
    Task<Result<int, PatientRepositoryError>> ActivateAsync(Guid code);
}
