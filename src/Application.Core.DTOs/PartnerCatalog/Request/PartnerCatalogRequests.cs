using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.PartnerCatalog.Request;

/// <summary>Partial update: omitted fields keep their current value.</summary>
public sealed record UpdatePartnerMedicationRequest
{
    public bool? IsEnabled { get; init; }

    [Range(0, 999999999, ErrorMessage = "ReorderLevel must be zero or greater.")]
    public decimal? ReorderLevel { get; init; }
}

/// <summary>Partial update: omitted fields keep their current value. Enabling requires both prices above zero.</summary>
public sealed record UpdatePartnerSpecialtyRequest
{
    public bool? IsEnabled { get; init; }

    [Range(0, 99999999, ErrorMessage = "PriceInPerson must be zero or greater.")]
    public decimal? PriceInPerson { get; init; }

    [Range(0, 99999999, ErrorMessage = "PriceVirtual must be zero or greater.")]
    public decimal? PriceVirtual { get; init; }
}

/// <summary>Partial update: omitted fields keep their current value. <c>ClearCoverageOverride</c> goes back to the catalog default.</summary>
public sealed record UpdatePartnerInsuranceRequest
{
    public bool? IsEnabled { get; init; }

    [Range(0, 100, ErrorMessage = "CoveragePercentageOverride must be between 0 and 100.")]
    public decimal? CoveragePercentageOverride { get; init; }

    public bool ClearCoverageOverride { get; init; }
}
