using Application.Core.DTOs.Clinic.Errors;
using Application.Core.DTOs.Clinic.Request;
using Application.Core.DTOs.Clinic.Response;
using BindSharp;

namespace Application.Core.Interfaces.Clinic;

public interface IClinic
{
    /// <summary>Returns the partner's default clinic.</summary>
    Task<Result<ClinicResponse, ClinicError>> GetDefaultAsync();

    /// <summary>Updates the operational fields of the default clinic.</summary>
    Task<Result<ClinicResponse, ClinicError>> UpdateDefaultAsync(UpdateClinicRequest request);
}
