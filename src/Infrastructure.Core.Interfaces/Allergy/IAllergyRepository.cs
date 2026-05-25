using BindSharp;
using Infrastructure.Core.DTOs.Allergy;
using Infrastructure.Core.Models.Allergy;

namespace Infrastructure.Core.Interfaces.Allergy;

public interface IAllergyRepository
{
    /// <summary>Returns one page of allergies, with a total count via window function.</summary>
    Task<Result<IEnumerable<AllergyRow>, AllergyRepositoryError>> GetPageAsync(int offset, int limit, string? search);

    /// <summary>Inserts a new allergy and returns the created row, or <c>null</c> on unexpected failure.</summary>
    Task<Result<AllergyRow?, AllergyRepositoryError>> InsertAsync(string name, string? description);

    /// <summary>Updates an active allergy by code. Returns <c>null</c> when no matching active row exists.</summary>
    Task<Result<AllergyRow?, AllergyRepositoryError>> UpdateAsync(Guid code, string name, string? description);

    /// <summary>Soft-deletes an active allergy. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, AllergyRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Reactivates a previously deactivated allergy. Returns the number of affected rows (0 = not found or already active).</summary>
    Task<Result<int, AllergyRepositoryError>> ActivateAsync(Guid code);
}
