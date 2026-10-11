namespace Application.Core.DTOs.Insurance.Response;

/// <summary>A shared-catalog insurer merged with this partner's enablement and coverage override.</summary>
public sealed class InsuranceCatalogItemResponse
{
    public required Guid InsuranceCode { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsEnabled { get; init; }
    public required decimal DefaultCoveragePercentage { get; init; }
    public decimal? CoveragePercentageOverride { get; init; }

    /// <summary>The percentage in force for this partner: the override when set, otherwise the catalog default.</summary>
    public required decimal CoveragePercentage { get; init; }
}
