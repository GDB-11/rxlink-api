using BindSharp;
using Infrastructure.Core.DTOs.Clinic;
using Infrastructure.Core.Models.Clinic;

namespace Infrastructure.Core.Interfaces.Clinic;

public interface IClinicRepository
{
    /// <summary>Returns the active default clinic, or <c>null</c> when none exists.</summary>
    Task<Result<ClinicRow?, ClinicRepositoryError>> GetDefaultAsync();

    /// <summary>
    /// Updates the operational fields of the active default clinic and returns the updated row,
    /// or <c>null</c> when none exists.
    /// </summary>
    Task<Result<ClinicRow?, ClinicRepositoryError>> UpdateDefaultAsync(
        string? address, string? district, string? province, string? department,
        string? phone, string? email, string? website, string? facilityRegistrationCode);
}
