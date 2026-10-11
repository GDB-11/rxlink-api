namespace Application.Core.Config;

/// <summary>Behaviour of the in-memory snapshot of the shared catalog (Phase 6).</summary>
public sealed record CatalogConfig
{
    /// <summary>How often <c>CatalogVersion</c> is polled for changes. Used by the refresh service (6.2).</summary>
    public int RefreshSeconds { get; init; } = 60;
}
