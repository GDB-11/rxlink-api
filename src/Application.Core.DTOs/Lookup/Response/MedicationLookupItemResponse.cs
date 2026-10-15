namespace Application.Core.DTOs.Lookup.Response;

public sealed class MedicationLookupItemResponse
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public required string DefaultDose { get; init; }
    public required Guid DefaultAdministrationRouteCode { get; init; }

    /// <summary>Default clinic's balance as <c>InStock</c>, <c>Low</c> or <c>Out</c>; informative only, doctors may prescribe regardless.</summary>
    public required string StockStatus { get; init; }
}