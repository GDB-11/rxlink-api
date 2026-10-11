using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Mfa.Request;

public sealed record ConfirmMfaRequest
{
    [Required(ErrorMessage = "Code is required.")]
    public required string Code { get; init; }
}
