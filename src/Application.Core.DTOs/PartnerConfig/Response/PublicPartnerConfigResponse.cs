namespace Application.Core.DTOs.PartnerConfig.Response;

/// <summary>Unauthenticated, PHI-free identity of this deployment for the login screen, headers and the mobile app.</summary>
public sealed class PublicPartnerConfigResponse
{
    public required Guid PartnerCode { get; init; }
    public required string Slug { get; init; }
    public required string DisplayName { get; init; }
    /// <summary>Relative URL with the logo hash as <c>v</c>, so it can be cached indefinitely; <c>null</c> when there is no logo.</summary>
    public string? LogoUrl { get; init; }
    public string? BrandPrimaryColor { get; init; }
    public string? BrandAccentColor { get; init; }
    public required string Country { get; init; }
    public required string Timezone { get; init; }
    public required string Currency { get; init; }
    public required string Locale { get; init; }
    public string? ClinicName { get; init; }
}
