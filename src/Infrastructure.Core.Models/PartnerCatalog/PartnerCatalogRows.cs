namespace Infrastructure.Core.Models.PartnerCatalog;

public sealed class PartnerMedicationRow
{
    public required Guid MedicationCode { get; init; }
    public required bool IsEnabled { get; init; }
    public decimal? ReorderLevel { get; init; }

    /// <summary>Default clinic's balance; only filled by the list query (the single-item queries leave it <c>null</c>).</summary>
    public decimal? StockQuantity { get; init; }
}

public sealed class PartnerSpecialtyRow
{
    public required Guid SpecialtyCode { get; init; }
    public required bool IsEnabled { get; init; }
    public required decimal PriceInPerson { get; init; }
    public required decimal PriceVirtual { get; init; }
}

public sealed class PartnerInsuranceRow
{
    public required Guid InsuranceCode { get; init; }
    public required bool IsEnabled { get; init; }
    public decimal? CoveragePercentageOverride { get; init; }
}
