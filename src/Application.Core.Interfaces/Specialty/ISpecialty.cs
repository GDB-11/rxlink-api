using Application.Core.DTOs.Specialty.Errors;
using Application.Core.DTOs.Specialty.Response;
using BindSharp;


namespace Application.Core.Interfaces.Specialty;

public interface ISpecialty
{
    Task<Result<IEnumerable<SpecialtyWithDoctorCountResponse>, SpecialtyError>> GetAllActiveWithDoctorCountAsync();

    Task<Result<IEnumerable<DoctorSummaryResponse>, SpecialtyError>> GetDoctorsBySpecialtyCodeAsync(Guid specialtyCode,
        string? search = null);
}