using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.CommunicationPreference.Request;

/// <summary>Full replacement of the patient's preferences: a <c>null</c> alternative contact clears it.</summary>
public sealed class UpdateCommunicationPreferenceRequest
{
    public required bool AllowEmail { get; init; }
    public required bool AllowSms { get; init; }

    /// <summary>Confidential communications: used instead of the e-mail on the person record.</summary>
    [MaxLength(254)]
    public string? AlternativeEmail { get; init; }

    /// <summary>Confidential communications: used instead of the phone on the person record.</summary>
    [MaxLength(20)]
    public string? AlternativePhone { get; init; }
}
