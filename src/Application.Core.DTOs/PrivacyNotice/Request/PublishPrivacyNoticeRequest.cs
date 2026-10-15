using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.PrivacyNotice.Request;

public sealed class PublishPrivacyNoticeRequest
{
    [Required]
    [MaxLength(200)]
    public required string Title { get; init; }

    /// <summary>Full text of the notice. A published version is never edited; a correction is a new version.</summary>
    [Required]
    [MaxLength(50000)]
    public required string Body { get; init; }

    /// <summary>Date (Lima) from which this version is the current one. Cannot be in the past more than today.</summary>
    public required DateOnly EffectiveFrom { get; init; }
}
