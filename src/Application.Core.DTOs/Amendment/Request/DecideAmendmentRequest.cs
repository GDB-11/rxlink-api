using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Amendment.Request;

public sealed class DecideAmendmentRequest
{
    public required bool Accepted { get; init; }

    /// <summary>Why the clinician accepts or denies. Mandatory in both cases (164.526(d)).</summary>
    [Required]
    [MinLength(5)]
    [MaxLength(1000)]
    public required string Reason { get; init; }

    /// <summary>Text appended to the record when accepted. Required if <see cref="Accepted"/>, ignored otherwise.</summary>
    [MaxLength(2000)]
    public string? AmendmentText { get; init; }
}
