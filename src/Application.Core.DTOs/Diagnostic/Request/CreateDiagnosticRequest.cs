using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Diagnostic.Request;

public sealed record CreateDiagnosticRequest
{
    [Required(ErrorMessage = "AppointmentCode is required.")]
    public required Guid AppointmentCode { get; init; }

    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    public required string Description { get; init; }

    [Required(ErrorMessage = "DiagnosedAt is required.")]
    public required DateOnly DiagnosedAt { get; init; }

    public string? Notes { get; init; }
}