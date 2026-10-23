namespace Application.Core.Config;

public sealed record JwtConfig
{
    public required string SecretKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int AccessTokenExpiryMinutes { get; init; }
    public int RefreshTokenExpiryMinutes { get; init; }

    /// <summary>Patient (mobile) access token lifetime, decision D31: 15 minutes.</summary>
    public int PatientAccessTokenExpiryMinutes { get; init; } = 15;

    /// <summary>Patient refresh token lifetime, sliding on every rotation, decision D31: 14 days.</summary>
    public int PatientRefreshTokenExpiryMinutes { get; init; } = 14 * 24 * 60;
}