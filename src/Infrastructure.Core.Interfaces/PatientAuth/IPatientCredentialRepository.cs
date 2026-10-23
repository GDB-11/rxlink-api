using BindSharp;
using Infrastructure.Core.DTOs.PatientAuth;
using Infrastructure.Core.Models.PatientAuth;

namespace Infrastructure.Core.Interfaces.PatientAuth;

public interface IPatientCredentialRepository
{
    Task<Result<PatientRegistrationCheckRow?, PatientCredentialError>> GetByDocumentAsync(Guid documentTypeCode,
        string documentNumber, DateTime birthDate);

    Task<Result<PatientRegistrationCheckRow?, PatientCredentialError>> GetByPersonCodeAsync(Guid personCode);
    Task<Result<PatientCredential?, PatientCredentialError>> GetByEmailAsync(string email);

    Task<Result<PatientCredential, PatientCredentialError>> CreatePersonAndPatientAsync(NewPatientData data,
        string passwordHash);

    Task<Result<PatientCredential, PatientCredentialError>> CreatePatientForPersonAsync(Guid personCode,
        string passwordHash);

    Task<Result<PatientCredential, PatientCredentialError>> AddCredentialsAsync(Guid personCode, string passwordHash);

    /// <summary>Stores the first refresh token of a new session (login / registration).</summary>
    Task<Result<Unit, PatientCredentialError>> StoreRefreshTokenAsync(Guid patientCode, Guid familyCode,
        string tokenHash, DateTime expiresAt, DateTime now);

    /// <summary>
    /// Atomically consumes <paramref name="tokenHash"/> and stores its successor in the same session family.
    /// <c>null</c> when the token is unknown, already used, revoked or expired, or the patient is inactive.
    /// </summary>
    Task<Result<PatientCredential?, PatientCredentialError>> RotateRefreshTokenAsync(string tokenHash,
        string newTokenHash, DateTime newExpiresAt, DateTime now);

    /// <summary>Revokes the whole family of an already-used token (replay). Returns the number of tokens revoked.</summary>
    Task<Result<int, PatientCredentialError>> RevokeFamilyOfUsedTokenAsync(string tokenHash, DateTime now);

    /// <summary>Revokes every live session of the patient (logout, password change).</summary>
    Task<Result<Unit, PatientCredentialError>> RevokeAllSessionsAsync(Guid patientCode, DateTime now);
}
