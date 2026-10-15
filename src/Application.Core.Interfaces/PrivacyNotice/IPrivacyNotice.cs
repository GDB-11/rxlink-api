using Application.Core.DTOs.PrivacyNotice.Errors;
using Application.Core.DTOs.PrivacyNotice.Request;
using Application.Core.DTOs.PrivacyNotice.Response;
using BindSharp;

namespace Application.Core.Interfaces.PrivacyNotice;

/// <summary>Notice of Privacy Practices (45 CFR 164.520): versions and patient acknowledgments. Append-only.</summary>
public interface IPrivacyNotice
{
    /// <summary>Publishes a new version of the notice (administrators).</summary>
    Task<Result<PrivacyNoticeResponse, PrivacyNoticeError>> PublishAsync(
        PublishPrivacyNoticeRequest request, Guid publishedByUserCode);

    /// <summary>All published versions, newest first.</summary>
    Task<Result<PrivacyNoticePageResponse, PrivacyNoticeError>> GetPageAsync(PrivacyNoticePageRequest request);

    /// <summary>The notice in force today.</summary>
    Task<Result<PrivacyNoticeResponse, PrivacyNoticeError>> GetCurrentAsync();

    /// <summary>Notice in force plus the patient's acknowledgments, for staff.</summary>
    Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> GetStatusAsync(Guid patientCode);

    /// <summary>Records that staff handed the notice in force to the patient and it was acknowledged. Idempotent.</summary>
    Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> AcknowledgeAsync(
        Guid patientCode, Guid recordedByUserCode);

    Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> GetOwnStatusAsync(Guid patientCode);

    /// <summary>The patient acknowledges the notice in force themselves. Idempotent.</summary>
    Task<Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError>> AcknowledgeOwnAsync(Guid patientCode);
}
