using BindSharp;
using Infrastructure.Core.DTOs.Specialty;
using Infrastructure.Core.Models.Specialty;

namespace Infrastructure.Core.Interfaces.Specialty;

public interface ISpecialtyRepository
{
    /// <summary>Returns one page of specialties, with a total count via window function.</summary>
    Task<Result<IEnumerable<SpecialtyRow>, SpecialtyRepositoryError>> GetPageAsync(int offset, int limit, string? search);

    /// <summary>Inserts a new specialty and returns the created row, or <c>null</c> on unexpected failure.</summary>
    Task<Result<SpecialtyRow?, SpecialtyRepositoryError>> InsertAsync(string name);

    /// <summary>Updates an active specialty by code. Returns <c>null</c> when no matching active row exists.</summary>
    Task<Result<SpecialtyRow?, SpecialtyRepositoryError>> UpdateAsync(
        Guid code, string name);

    /// <summary>Soft-deletes an active specialty. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, SpecialtyRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);
    
    /// <summary>Reactivates a previously deactivated specialty. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, SpecialtyRepositoryError>> ActivateAsync(Guid code);
}