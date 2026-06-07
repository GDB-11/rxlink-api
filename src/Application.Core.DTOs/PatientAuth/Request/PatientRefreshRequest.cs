using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.PatientAuth.Request;

public sealed record PatientRefreshRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}
