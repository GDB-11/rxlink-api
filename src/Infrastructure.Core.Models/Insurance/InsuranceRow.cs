namespace Infrastructure.Core.Models.Insurance;

public sealed class InsuranceRow
{
    public required Guid InsuranceCode { get; init; }
    public required string Name { get; init; }
    public required decimal CoveragePercentage { get; init; }
    public required bool IsActive { get; init; }
    public required long TotalCount { get; init; }
}
