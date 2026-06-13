namespace Application.Core.DTOs.Account;

public sealed record UserInfo
{
    public required Guid UserCode { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string RoleName { get; init; }
}