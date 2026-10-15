using Application.Core.DTOs.CommunicationPreference.Errors;
using Application.Core.DTOs.CommunicationPreference.Request;
using Application.Core.DTOs.CommunicationPreference.Response;
using BindSharp;

namespace Application.Core.Interfaces.CommunicationPreference;

/// <summary>Restrictions and confidential communications (45 CFR 164.522), honoured by the notification worker.</summary>
public interface ICommunicationPreference
{
    Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> GetAsync(Guid patientCode);

    Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> UpdateAsync(
        Guid patientCode, UpdateCommunicationPreferenceRequest request, Guid updatedByUserCode);

    /// <summary>The patient's own preferences.</summary>
    Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> GetOwnAsync(Guid patientCode);

    Task<Result<CommunicationPreferenceResponse, CommunicationPreferenceError>> UpdateOwnAsync(
        Guid patientCode, UpdateCommunicationPreferenceRequest request);
}
