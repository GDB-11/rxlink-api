namespace Application.Core.DTOs.Lookup.Response;

public sealed class InsuranceLookupItemResponse
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public required decimal CoveragePercentage { get; init; }
}
