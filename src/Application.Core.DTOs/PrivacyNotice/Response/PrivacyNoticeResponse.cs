namespace Application.Core.DTOs.PrivacyNotice.Response;

public sealed class PrivacyNoticeResponse
{
    public required Guid PrivacyNoticeCode { get; init; }
    public required int Version { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required DateOnly EffectiveFrom { get; init; }
    public required DateTimeOffset PublishedAt { get; init; }
}

public sealed class PrivacyNoticePageResponse
{
    public required List<PrivacyNoticeResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}

public sealed class PrivacyNoticeAcknowledgmentResponse
{
    public required Guid PrivacyNoticeCode { get; init; }
    public required int Version { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset AcknowledgedAt { get; init; }

    /// <summary>Staff member who captured it; <c>null</c> when the patient acknowledged it themselves.</summary>
    public string? RecordedByName { get; init; }
}

/// <summary>Where one patient stands with the Notice of Privacy Practices.</summary>
public sealed class PatientPrivacyNoticeStatusResponse
{
    /// <summary>The notice in force; <c>null</c> when none has been published.</summary>
    public PrivacyNoticeResponse? Current { get; init; }

    /// <summary>The patient has acknowledged <see cref="Current"/>.</summary>
    public required bool CurrentAcknowledged { get; init; }

    /// <summary>Every acknowledgment of the patient, newest first.</summary>
    public required List<PrivacyNoticeAcknowledgmentResponse> Acknowledgments { get; init; }
}
