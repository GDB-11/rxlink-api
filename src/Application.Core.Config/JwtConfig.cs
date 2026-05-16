namespace Application.Core.Config;

public sealed record JwtConfig
{
    public required string SecretKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int AccessTokenExpiryMinutes { get; init; }
    public int RefreshTokenExpiryMinutes { get; init; }
}