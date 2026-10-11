using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Auth.Request;

/// <summary>Second login step: the short-lived token returned by login plus the authenticator code.</summary>
public sealed record MfaVerifyRequest
{
    [Required(ErrorMessage = "MFA token is required.")]
    public required string MfaToken { get; init; }

    [Required(ErrorMessage = "Code is required.")]
    public required string Code { get; init; }
}
