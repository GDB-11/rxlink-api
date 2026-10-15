namespace Application.Core.DTOs.CommunicationPreference.Response;

public sealed class CommunicationPreferenceResponse
{
    public required Guid PatientCode { get; init; }
    public required bool AllowEmail { get; init; }
    public required bool AllowSms { get; init; }
    public string? AlternativeEmail { get; init; }
    public string? AlternativePhone { get; init; }

    /// <summary><c>null</c> while the patient has never set a preference (defaults apply).</summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}
