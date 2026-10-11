using BindSharp;
using Infrastructure.Core.DTOs.Specialty;
using Infrastructure.Core.Models.Specialty;

namespace Infrastructure.Core.Interfaces.Specialty;

public interface ISpecialtyRepository
{
    /// <summary>Returns all active specialties with their active doctor count.</summary>
    Task<Result<IEnumerable<SpecialtyWithDoctorCountRow>, SpecialtyRepositoryError>> GetAllActiveWithDoctorCountAsync();

    /// <summary>
    /// Returns active doctors for the given specialty code.
    /// Returns <c>null</c> when the specialty does not exist or is inactive.
    /// Returns an empty enumerable when the specialty exists but has no active doctors.
    /// </summary>
    Task<Result<IEnumerable<DoctorSummaryRow>?, SpecialtyRepositoryError>> GetDoctorsBySpecialtyCodeAsync(
        Guid specialtyCode, string? search = null);
}