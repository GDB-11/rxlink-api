namespace Infrastructure.Core.Models.PartnerConfig;

/// <summary>Deployment telemetry sent to the catalog platform (architecture doc §5.5). No PHI, only versions.</summary>
public sealed class HeartbeatPayload
{
    public required string ApiVersion { get; init; }
    /// <summary>Not tracked in the partner DB yet, so it is <c>null</c> until a schema-version table exists.</summary>
    public int? DbSchemaVersion { get; init; }
    public required SchemaRange SupportedCatalogSchemas { get; init; }
    public int? CatalogSchemaVersionSeen { get; init; }
    public long? CatalogVersionSeen { get; init; }
}

public sealed record SchemaRange(int Min, int Max);
