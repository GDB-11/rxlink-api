using BindSharp;
using Infrastructure.Core.DTOs.Catalog;

namespace Infrastructure.Core.Interfaces.Catalog;

/// <summary>Holds the current <see cref="CatalogSnapshot"/> and swaps it atomically.</summary>
public interface ICatalogSnapshotStore
{
    /// <summary>The last successfully loaded snapshot, or <c>null</c> if none has loaded yet (cold start).</summary>
    CatalogSnapshot? Current { get; }

    /// <summary>
    /// Loads a fresh snapshot and swaps it in. On failure the previous snapshot (if any) stays current.
    /// Concurrent calls are serialised.
    /// </summary>
    Task<Result<CatalogSnapshot, CatalogRepositoryError>> RefreshAsync();

    /// <summary>
    /// Polling entry point: reads only the <c>CatalogVersion</c> row and reloads the tables when it differs from the
    /// current snapshot (or when there is no snapshot yet). Unchanged: no other query runs.
    /// </summary>
    Task<Result<CatalogSnapshot, CatalogRepositoryError>> RefreshIfChangedAsync();

    /// <summary>
    /// When the latest load/poll attempt failed, or <c>null</c> if it succeeded. With a snapshot present, a value
    /// here means the API is serving the last good one (possibly stale).
    /// </summary>
    DateTimeOffset? LastFailureAt { get; }
}
