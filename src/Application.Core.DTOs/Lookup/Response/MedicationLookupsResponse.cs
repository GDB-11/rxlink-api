namespace Application.Core.DTOs.Lookup.Response;

public sealed class MedicationLookupsResponse
{
    public required IReadOnlyList<LookupItemResponse> PharmaceuticalForms { get; init; }
    public required IReadOnlyList<LookupItemResponse> AdministrationRoutes { get; init; }
}