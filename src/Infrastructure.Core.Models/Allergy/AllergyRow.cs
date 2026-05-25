namespace Infrastructure.Core.Models.Allergy;

/// <summary>
/// Flat row returned by allergy queries.
/// <c>TotalCount</c> is populated by the paginated query via a window function;
/// single-row operations (insert/update) return 0.
/// </summary>
public sealed class AllergyRow
{
    public required Guid AllergyCode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required long TotalCount { get; init; }
}
