namespace Application.Core.DTOs.Medication.Request;

public sealed record UpdateMedicationRequest
{
    public required int PharmaceuticalFormId { get; init; }
    public required int AdministrationRouteId { get; init; }
    public required string GenericName { get; init; }
    public string? CommercialName { get; init; }
    public required string Concentration { get; init; }
}
