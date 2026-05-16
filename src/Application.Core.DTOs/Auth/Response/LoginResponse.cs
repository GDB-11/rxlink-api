using Application.Core.DTOs.Account;

namespace Application.Core.DTOs.Auth.Response;

public sealed record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required UserInfo User { get; init; }
}