namespace Infrastructure.Core.Models.Medication;

/// <summary>
/// Flat row returned by medication queries.
/// <c>TotalCount</c> is populated by the paginated query via a window function;
/// single-row operations (insert/update) return 0.
/// </summary>
public sealed class MedicationRow
{
    public required Guid MedicationCode { get; init; }
    public required int PharmaceuticalFormId { get; init; }
    public required string PharmaceuticalFormName { get; init; }
    public required int AdministrationRouteId { get; init; }
    public required string AdministrationRouteName { get; init; }
    public required string GenericName { get; init; }
    public string? CommercialName { get; init; }
    public required string Concentration { get; init; }
    public required bool IsActive { get; init; }
    public required long TotalCount { get; init; }
}
