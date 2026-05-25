using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Medication.Request;

public sealed record UpdateMedicationRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "PharmaceuticalFormId must be a valid identifier.")]
    public required int PharmaceuticalFormId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "AdministrationRouteId must be a valid identifier.")]
    public required int AdministrationRouteId { get; init; }

    [Required(ErrorMessage = "GenericName is required.")]
    [MaxLength(200, ErrorMessage = "GenericName must not exceed 200 characters.")]
    public required string GenericName { get; init; }

    [MaxLength(200, ErrorMessage = "CommercialName must not exceed 200 characters.")]
    public string? CommercialName { get; init; }

    [Required(ErrorMessage = "Concentration is required.")]
    [MaxLength(50, ErrorMessage = "Concentration must not exceed 50 characters.")]
    public required string Concentration { get; init; }
}
