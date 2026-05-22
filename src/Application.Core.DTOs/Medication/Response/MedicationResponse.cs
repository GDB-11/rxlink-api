namespace Application.Core.DTOs.Medication.Response;

public sealed class MedicationResponse
{
    public required Guid MedicationCode { get; init; }
    public required int PharmaceuticalFormId { get; init; }
    public required string PharmaceuticalFormName { get; init; }
    public required int AdministrationRouteId { get; init; }
    public required string AdministrationRouteName { get; init; }
    public required string GenericName { get; init; }
    public string? CommercialName { get; init; }
    public required string Concentration { get; init; }
    public required bool IsActive { get; init; }
}
