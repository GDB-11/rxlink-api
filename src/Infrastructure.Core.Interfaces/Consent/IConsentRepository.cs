using BindSharp;
using Infrastructure.Core.DTOs.Consent;
using Infrastructure.Core.Models.Consent;

namespace Infrastructure.Core.Interfaces.Consent;

public interface IConsentRepository
{
    /// <summary>Appends a consent. <c>null</c> when no active patient has that code.</summary>
    Task<Result<ConsentRow?, ConsentRepositoryError>> InsertAsync(
        Guid patientCode, string consentType, string description, DateTimeOffset grantedAt, Guid recordedByUserCode);

    /// <summary>
    /// Revokes a consent of that patient. <c>null</c> when it does not exist or was already revoked.
    /// <paramref name="revokedByUserCode"/> <c>null</c> = the patient.
    /// </summary>
    Task<Result<ConsentRow?, ConsentRepositoryError>> RevokeAsync(
        Guid patientCode, Guid consentCode, string reason, Guid? revokedByUserCode);

    /// <summary>Consents of one patient, newest first.</summary>
    Task<Result<IEnumerable<ConsentRow>, ConsentRepositoryError>> GetPageAsync(Guid patientCode, int offset, int limit);
}
