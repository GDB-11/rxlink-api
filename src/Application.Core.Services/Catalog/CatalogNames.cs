using Application.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Catalog;

namespace Application.Core.Services.Catalog;

public sealed class CatalogNames : ICatalogNames
{
    private readonly ICatalogSnapshotStore _store;

    public CatalogNames(ICatalogSnapshotStore store)
    {
        _store = store;
    }

    /// <inheritdoc/>
    public string Sex(Guid code) =>
        _store.Current is { } c && c.Sexes.TryGetValue(code, out var row) ? row.Name : string.Empty;

    /// <inheritdoc/>
    public string DocumentType(Guid code) =>
        _store.Current is { } c && c.DocumentTypes.TryGetValue(code, out var row) ? row.Name : string.Empty;

    /// <inheritdoc/>
    public string Specialty(Guid code) =>
        _store.Current is { } c && c.Specialties.TryGetValue(code, out var row) ? row.Name : string.Empty;

    /// <inheritdoc/>
    public string Allergy(Guid code) =>
        _store.Current is { } c && c.Allergies.TryGetValue(code, out var row) ? row.Name : string.Empty;

    /// <inheritdoc/>
    public string AllergySeverity(Guid code) =>
        _store.Current is { } c && c.AllergySeverities.TryGetValue(code, out var row) ? row.Name : string.Empty;
}
