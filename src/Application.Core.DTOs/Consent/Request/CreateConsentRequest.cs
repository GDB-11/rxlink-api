using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Consent.Request;

public sealed class CreateConsentRequest
{
    /// <summary>See <c>ConsentTypes</c>.</summary>
    [Required]
    [MaxLength(20)]
    public required string ConsentType { get; init; }

    /// <summary>What the patient authorized (scope, recipient, purpose). Never clinical content.</summary>
    [Required]
    [MaxLength(500)]
    public required string Description { get; init; }

    /// <summary>When the patient signed or gave the authorization. Cannot be in the future.</summary>
    public required DateTimeOffset GrantedAt { get; init; }
}

public sealed class RevokeConsentRequest
{
    [Required]
    [MaxLength(500)]
    public required string Reason { get; init; }
}

public sealed record ConsentPageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
