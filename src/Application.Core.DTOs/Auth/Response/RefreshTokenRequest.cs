namespace Application.Core.DTOs.Auth.Response;

public sealed record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}