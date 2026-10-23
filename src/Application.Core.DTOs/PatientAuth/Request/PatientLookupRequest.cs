using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.PatientAuth.Request;

/// <summary>
/// Identity check before registration. The birth date is a second factor: the lookup answers
/// "found" only when document and birth date belong to the same person.
/// </summary>
public sealed record PatientLookupRequest
{
    [Required]
    public required Guid DocumentTypeCode { get; init; }

    [Required]
    [MaxLength(50)]
    public required string DocumentNumber { get; init; }

    [Required]
    public required DateOnly BirthDate { get; init; }
}
