using BindSharp;
using Infrastructure.Core.DTOs.Insurance;
using Infrastructure.Core.Models.Insurance;

namespace Infrastructure.Core.Interfaces.Insurance;

public interface IInsuranceRepository
{
    /// <summary>Returns one page of insurances, with a total count via window function.</summary>
    Task<Result<IEnumerable<InsuranceRow>, InsuranceRepositoryError>> GetPageAsync(int offset, int limit,
        string? search);

    /// <summary>Inserts a new insurance and returns the created row, or <c>null</c> on unexpected failure.</summary>
    Task<Result<InsuranceRow?, InsuranceRepositoryError>> InsertAsync(string name, decimal coveragePercentage);

    /// <summary>Updates an active insurance by code. Returns <c>null</c> when no matching active row exists.</summary>
    Task<Result<InsuranceRow?, InsuranceRepositoryError>> UpdateAsync(
        Guid code, string name, decimal coveragePercentage);

    /// <summary>Soft-deletes an active insurance. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, InsuranceRepositoryError>> DeactivateAsync(Guid code);

    /// <summary>Reactivates a previously deactivated insurance. Returns the number of affected rows (0 = not found or already inactive).</summary>
    Task<Result<int, InsuranceRepositoryError>> ActivateAsync(Guid code);
}
