namespace Application.Core.Config;

/// <summary>Connection to <c>rxlink-catalog-api</c>, used only for the best-effort heartbeat (Phase 7.3).</summary>
public sealed record CatalogApiConfig
{
    /// <summary>Base URL of the catalog API. Empty disables the heartbeat.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>Per-partner API key (shown once at provisioning). Empty disables the heartbeat.</summary>
    public string? ApiKey { get; init; }

    public int HeartbeatMinutes { get; init; } = 60;

    /// <summary>Catalog <c>SchemaVersion</c> range this build understands (reported so the platform knows who is ready).</summary>
    public int SupportedCatalogSchemaMin { get; init; } = 1;
    public int SupportedCatalogSchemaMax { get; init; } = 1;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
}
