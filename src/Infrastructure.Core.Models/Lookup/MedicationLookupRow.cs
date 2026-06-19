namespace Infrastructure.Core.Models.Lookup;

public sealed class MedicationLookupRow
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public required string DefaultDose { get; init; }
    public required Guid DefaultAdministrationRouteCode { get; init; }
}