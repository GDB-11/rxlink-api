namespace Application.Core.DTOs.Lookup.Response;

public sealed class PrescriptionLookupsResponse
{
    public required IReadOnlyList<GuidLookupItemResponse> PrescriptionStatuses { get; init; }
    public required IReadOnlyList<MedicationLookupItemResponse> Medications { get; init; }
    public required IReadOnlyList<GuidLookupItemResponse> AdministrationRoutes { get; init; }
    public required IReadOnlyList<GuidLookupItemResponse> Frequencies { get; init; }
}