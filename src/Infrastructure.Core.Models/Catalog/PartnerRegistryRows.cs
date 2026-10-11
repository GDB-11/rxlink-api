namespace Infrastructure.Core.Models.Catalog;

/// <summary>This deployment's own registry row (from the <c>partner_self</c> view).</summary>
public sealed class PartnerIdentityRow
{
    public required Guid PartnerCode { get; init; }
    public required string Slug { get; init; }
    public required string DisplayName { get; init; }
    public required string Country { get; init; }
    public required string Timezone { get; init; }
    public required string Currency { get; init; }
    public required string Locale { get; init; }
    public string? BrandPrimaryColor { get; init; }
    public string? BrandAccentColor { get; init; }
}

/// <summary>This deployment's logo (from the <c>partner_logo_self</c> view).</summary>
public sealed class PartnerLogoRow
{
    public required string ContentType { get; init; }
    public required byte[] Data { get; init; }
    public required string Sha256 { get; init; }
}
