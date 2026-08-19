namespace Infrastructure.Core.Models.Lookup;

public sealed class InsuranceLookupRow
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
    public required decimal CoveragePercentage { get; init; }
}
