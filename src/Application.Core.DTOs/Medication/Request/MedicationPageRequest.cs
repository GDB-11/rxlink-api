namespace Application.Core.DTOs.Medication.Request;

public sealed record MedicationPageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }

    /// <summary>Only items that are active in the catalog and enabled by this partner.</summary>
    public bool EnabledOnly { get; init; }
}