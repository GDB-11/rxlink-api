using BindSharp;
using Infrastructure.Core.DTOs.Specialty;
using Infrastructure.Core.Models.Specialty;

namespace Infrastructure.Core.Interfaces.Specialty;

public interface ISpecialtyRepository
{
    /// <summary>Returns the active doctor count per specialty code (only codes that have at least one doctor).</summary>
    Task<Result<IReadOnlyList<SpecialtyDoctorCountRow>, SpecialtyRepositoryError>> GetDoctorCountsAsync();

    /// <summary>Returns the active doctors assigned to the given specialty code; empty when there are none.</summary>
    Task<Result<IReadOnlyList<DoctorSummaryRow>, SpecialtyRepositoryError>> GetDoctorsBySpecialtyCodeAsync(
        Guid specialtyCode, string? search = null);
}
