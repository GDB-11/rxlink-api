namespace Application.Core.DTOs.Lookup.Response;

public sealed class MedicationLookupItemResponse
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public required string DefaultDose { get; init; }
    public required Guid DefaultAdministrationRouteCode { get; init; }
}