namespace Application.Core.DTOs.Insurance.Response;

public sealed class InsuranceResponse
{
    public required Guid InsuranceCode { get; init; }
    public required string Name { get; init; }
    public required decimal CoveragePercentage { get; init; }
    public required bool IsActive { get; init; }
}
