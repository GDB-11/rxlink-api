namespace Application.Core.DTOs.PartnerCatalog.Response;

public sealed class PartnerMedicationResponse
{
    public required Guid MedicationCode { get; init; }
    public required bool IsEnabled { get; init; }
    public decimal? ReorderLevel { get; init; }
}

public sealed class PartnerSpecialtyResponse
{
    public required Guid SpecialtyCode { get; init; }
    public required bool IsEnabled { get; init; }
    public required decimal PriceInPerson { get; init; }
    public required decimal PriceVirtual { get; init; }
}

public sealed class PartnerInsuranceResponse
{
    public required Guid InsuranceCode { get; init; }
    public required bool IsEnabled { get; init; }
    public decimal? CoveragePercentageOverride { get; init; }
}
