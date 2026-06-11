using Application.Core.DTOs.PatientAuth.Errors;
using Application.Core.DTOs.PatientAuth.Request;
using Application.Core.DTOs.PatientAuth.Response;
using BindSharp;

namespace Application.Core.Interfaces.PatientAuth;

public interface IPatientAuthentication
{
    Task<Result<PatientLookupResponse, PatientAuthError>> LookupAsync(Guid documentTypeCode, string documentNumber);
    Task<Result<PatientAuthResponse, PatientAuthError>> RegisterAsync(RegisterPatientRequest request);
    Task<Result<PatientAuthResponse, PatientAuthError>> LoginAsync(PatientLoginRequest request);
    Task<Result<PatientAuthResponse, PatientAuthError>> RefreshAsync(PatientRefreshRequest request);
    Task<Result<Unit, PatientAuthError>> LogoutAsync(Guid patientCode);
}