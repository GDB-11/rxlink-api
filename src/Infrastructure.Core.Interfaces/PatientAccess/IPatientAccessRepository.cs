using BindSharp;
using Infrastructure.Core.DTOs.PatientAccess;
using Infrastructure.Core.Models.PatientAccess;

namespace Infrastructure.Core.Interfaces.PatientAccess;

public interface IPatientAccessRepository
{
    /// <summary>Reads the facts the access policy needs for a staff user and a patient.</summary>
    Task<Result<PatientAccessFactsRow, PatientAccessRepositoryError>> GetFactsAsync(
        Guid userCode, Guid patientCode);

    /// <summary>
    /// Opens the patient to the user for <paramref name="ttlMinutes"/> and emits the
    /// <c>Record.BreakGlassAccess</c> event in the same transaction. Nothing is written when the patient is
    /// already in the care team. Returns <c>null</c> when the medical record number matches no active patient.
    /// </summary>
    Task<Result<BreakGlassGrantRow?, PatientAccessRepositoryError>> GrantBreakGlassAsync(
        Guid userCode, string medicalRecordNumber, string reason, int ttlMinutes);

    /// <param name="from">Inclusive lower bound.</param>
    /// <param name="toExclusive">Exclusive upper bound.</param>
    Task<Result<IEnumerable<BreakGlassReportRow>, PatientAccessRepositoryError>> GetBreakGlassReportAsync(
        DateTimeOffset from, DateTimeOffset toExclusive);
}
