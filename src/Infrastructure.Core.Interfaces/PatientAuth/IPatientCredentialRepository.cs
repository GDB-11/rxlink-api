using BindSharp;
using Infrastructure.Core.DTOs.PatientAuth;
using Infrastructure.Core.Models.PatientAuth;

namespace Infrastructure.Core.Interfaces.PatientAuth;

public interface IPatientCredentialRepository
{
    Task<Result<PatientRegistrationCheckRow?, PatientCredentialError>> GetByDocumentAsync(Guid documentTypeCode,
        string documentNumber);

    Task<Result<PatientRegistrationCheckRow?, PatientCredentialError>> GetByPersonCodeAsync(Guid personCode);
    Task<Result<PatientCredential?, PatientCredentialError>> GetByEmailAsync(string email);

    Task<Result<PatientCredential?, PatientCredentialError>> GetByRefreshTokenAsync(string tokenHash,
        DateTime currentDate);

    Task<Result<PatientCredential, PatientCredentialError>> CreatePersonAndPatientAsync(NewPatientData data,
        string passwordHash);

    Task<Result<PatientCredential, PatientCredentialError>> CreatePatientForPersonAsync(Guid personCode,
        string passwordHash);

    Task<Result<PatientCredential, PatientCredentialError>> AddCredentialsAsync(Guid personCode, string passwordHash);

    Task<Result<Unit, PatientCredentialError>> UpdateRefreshTokenAsync(Guid patientCode, string tokenHash,
        DateTime expiresAt);

    Task<Result<Unit, PatientCredentialError>> ClearRefreshTokenAsync(Guid patientCode);
}