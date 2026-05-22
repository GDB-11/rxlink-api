using BindSharp;
using Infrastructure.Core.DTOs.Medication;
using Infrastructure.Core.Models.Medication;

namespace Infrastructure.Core.Interfaces.Medication;

public interface IMedicationRepository
{
    /// <summary>Returns one page of medications, with a total count via window function.</summary>
    Task<Result<IEnumerable<MedicationRow>, MedicationRepositoryError>> GetPageAsync(int offset, int limit, string? search);

    /// <summary>Inserts a new medication and returns the created row, or <c>null</c> on unexpected failure.</summary>
    Task<Result<MedicationRow?, MedicationRepositoryError>> InsertAsync(
        int pharmaceuticalFormId, int administrationRouteId,
        string genericName, string? commercialName, string concentration);

    /// <summary>Updates an active medication by code. Returns <c>null</c> when no matching active row exists.</summary>
    Task<Result<MedicationRow?, MedicationRepositoryError>> UpdateAsync(
        Guid code, int pharmaceuticalFormId, int administrationRouteId,
        string genericName, string? commercialName, string concentration);

    /// <summary>Soft-deletes an active medication. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, MedicationRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);
}
