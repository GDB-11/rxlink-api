namespace Infrastructure.Core.Models.CommunicationPreference;

public sealed class CommunicationPreferenceRow
{
    public required Guid PatientCode { get; init; }
    public required bool AllowEmail { get; init; }
    public required bool AllowSms { get; init; }
    public string? AlternativeEmail { get; init; }
    public string? AlternativePhone { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
