using BindSharp;
using Infrastructure.Core.DTOs.Catalog;

namespace Infrastructure.Core.Interfaces.Catalog;

public static class CatalogSnapshotStoreExtensions
{
    /// <summary>
    /// The current snapshot, or <see cref="CatalogUnavailableError"/> when none has loaded. Catalog-dependent writes
    /// use this so they fail clearly instead of validating against nothing.
    /// </summary>
    public static Result<CatalogSnapshot, CatalogRepositoryError> GetRequired(this ICatalogSnapshotStore store) =>
        store.Current is { } snapshot
            ? Result<CatalogSnapshot, CatalogRepositoryError>.Success(snapshot)
            : Result<CatalogSnapshot, CatalogRepositoryError>.Failure(new CatalogUnavailableError());
}
