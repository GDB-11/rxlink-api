using BindSharp;
using Infrastructure.Core.DTOs.CommunicationPreference;
using Infrastructure.Core.Models.CommunicationPreference;

namespace Infrastructure.Core.Interfaces.CommunicationPreference;

public interface ICommunicationPreferenceRepository
{
    /// <summary>The patient's preferences (defaults when never set). <c>null</c> when no active patient has that code.</summary>
    Task<Result<CommunicationPreferenceRow?, CommunicationPreferenceRepositoryError>> GetAsync(Guid patientCode);

    /// <summary>Creates or replaces the preferences. <c>null</c> when no active patient has that code.</summary>
    Task<Result<CommunicationPreferenceRow?, CommunicationPreferenceRepositoryError>> UpsertAsync(
        Guid patientCode, bool allowEmail, bool allowSms, string? alternativeEmail, string? alternativePhone);
}
