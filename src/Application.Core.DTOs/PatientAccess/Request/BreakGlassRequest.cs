using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.PatientAccess.Request;

public sealed class BreakGlassRequest
{
    /// <summary>Exact medical record number (no partial matching, so the register cannot be browsed).</summary>
    [Required]
    [MaxLength(50)]
    public required string MedicalRecordNumber { get; init; }

    /// <summary>Why the caller needs a patient outside their care team. Stored and shown to the administrator.</summary>
    [Required]
    [MinLength(10)]
    [MaxLength(500)]
    public required string Reason { get; init; }
}
