using BindSharp;
using Infrastructure.Core.DTOs.PrivacyNotice;
using Infrastructure.Core.Models.PrivacyNotice;

namespace Infrastructure.Core.Interfaces.PrivacyNotice;

public interface IPrivacyNoticeRepository
{
    /// <summary>Appends the next version (highest + 1).</summary>
    Task<Result<PrivacyNoticeRow?, PrivacyNoticeRepositoryError>> PublishAsync(
        string title, string body, DateOnly effectiveFrom, Guid publishedByUserCode);

    Task<Result<IEnumerable<PrivacyNoticeRow>, PrivacyNoticeRepositoryError>> GetPageAsync(int offset, int limit);

    /// <summary>Highest version whose effective date has been reached (Lima); <c>null</c> when none.</summary>
    Task<Result<PrivacyNoticeRow?, PrivacyNoticeRepositoryError>> GetCurrentAsync();

    /// <summary>Acknowledgments of one patient, newest first.</summary>
    Task<Result<IEnumerable<PrivacyNoticeAcknowledgmentRow>, PrivacyNoticeRepositoryError>> GetAcknowledgmentsAsync(
        Guid patientCode);

    /// <summary>
    /// Acknowledges the notice in force. Returns the number of rows written: 0 when the patient is unknown, no notice
    /// is in force, or it was already acknowledged. <paramref name="recordedByUserCode"/> <c>null</c> = the patient.
    /// </summary>
    Task<Result<int, PrivacyNoticeRepositoryError>> AcknowledgeCurrentAsync(
        Guid patientCode, Guid? recordedByUserCode);
}
