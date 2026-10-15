namespace Application.Core.Interfaces.Catalog;

/// <summary>
/// Resolves display names of catalog items from the in-memory snapshot, so partner queries only carry codes.
/// Inactive items still resolve (history must keep its labels). An unknown code, or a cold start with no snapshot,
/// yields <c>string.Empty</c> (never throws).
/// </summary>
public interface ICatalogNames
{
    string Sex(Guid code);
    string DocumentType(Guid code);
    string Specialty(Guid code);
    string Allergy(Guid code);
    string AllergySeverity(Guid code);
}
