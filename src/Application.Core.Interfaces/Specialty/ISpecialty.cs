using Application.Core.DTOs.Specialty.Errors;
using Application.Core.DTOs.Specialty.Request;
using Application.Core.DTOs.Specialty.Response;
using BindSharp;


namespace Application.Core.Interfaces.Specialty;

public interface ISpecialty
{
    Task<Result<SpecialtyPageResponse, SpecialtyError>> GetPageAsync(SpecialtyPageRequest request);
    Task<Result<SpecialtyResponse, SpecialtyError>> CreateAsync(CreateSpecialtyRequest request);
    Task<Result<SpecialtyResponse, SpecialtyError>> UpdateAsync(Guid code, UpdateSpecialtyRequest request);
    Task<Result<Unit, SpecialtyError>> DeactivateAsync(Guid code, Guid performedByUserCode);
    Task<Result<Unit, SpecialtyError>> ActivateAsync(Guid code, Guid performedByUserCode);
    Task<Result<IEnumerable<SpecialtyWithDoctorCountResponse>, SpecialtyError>> GetAllActiveWithDoctorCountAsync();
    Task<Result<IEnumerable<DoctorSummaryResponse>, SpecialtyError>> GetDoctorsBySpecialtyCodeAsync(Guid specialtyCode);
}