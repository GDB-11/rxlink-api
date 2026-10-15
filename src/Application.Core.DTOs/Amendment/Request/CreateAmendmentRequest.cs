using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Amendment.Request;

public sealed class CreateAmendmentRequest
{
    /// <summary>Part of the record the patient wants corrected, see <c>AmendmentTargetAreas</c>.</summary>
    [Required]
    [MaxLength(20)]
    public required string TargetArea { get; init; }

    /// <summary>The patient's own words: what is wrong and what it should say.</summary>
    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public required string RequestedChange { get; init; }
}
