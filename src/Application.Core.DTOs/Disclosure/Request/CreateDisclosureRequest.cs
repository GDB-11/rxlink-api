using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Disclosure.Request;

public sealed class CreateDisclosureRequest
{
    /// <summary>When the information was handed over. Cannot be in the future.</summary>
    public required DateTimeOffset DisclosedAt { get; init; }

    [Required]
    [MaxLength(200)]
    public required string RecipientName { get; init; }

    /// <summary>See <c>DisclosureRecipientTypes</c>.</summary>
    [Required]
    [MaxLength(20)]
    public required string RecipientType { get; init; }

    [Required]
    [MaxLength(300)]
    public required string Purpose { get; init; }

    /// <summary>Kind of information disclosed (e.g. "resumen de atención"). Never a copy of the clinical content.</summary>
    [Required]
    [MaxLength(500)]
    public required string Description { get; init; }
}
