using Application.Core.DTOs.Consent.Errors;
using Application.Core.DTOs.Consent.Request;
using Application.Core.DTOs.Consent.Response;
using BindSharp;

namespace Application.Core.Interfaces.Consent;

/// <summary>
/// Authorizations for uses beyond treatment, payment and operations (45 CFR 164.508). Never edited or deleted:
/// the only change a record accepts is its revocation.
/// </summary>
public interface IConsent
{
    Task<Result<ConsentResponse, ConsentError>> RecordAsync(
        Guid patientCode, CreateConsentRequest request, Guid recordedByUserCode);

    Task<Result<ConsentResponse, ConsentError>> RevokeAsync(
        Guid patientCode, Guid consentCode, RevokeConsentRequest request, Guid revokedByUserCode);

    Task<Result<ConsentPageResponse, ConsentError>> GetPageAsync(Guid patientCode, ConsentPageRequest request);

    Task<Result<ConsentPageResponse, ConsentError>> GetOwnPageAsync(Guid patientCode, ConsentPageRequest request);

    /// <summary>The patient withdraws their own authorization.</summary>
    Task<Result<ConsentResponse, ConsentError>> RevokeOwnAsync(
        Guid patientCode, Guid consentCode, RevokeConsentRequest request);
}
