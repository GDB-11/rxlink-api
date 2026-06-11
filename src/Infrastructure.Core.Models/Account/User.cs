namespace Infrastructure.Core.Models.Account;

/// <summary>
/// Represents a system user in the context of authentication and credential management.
/// Combines columns from the <c>User</c> and <c>Person</c> tables.
/// </summary>
public sealed class User
{
    public required Guid UserCode { get; init; }
    public required Guid PersonCode { get; init; }
    public required string RoleName { get; init; }
    public int? SpecialtyId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string PasswordHash { get; init; }
    public string? LicenseNumber { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastAccess { get; init; }
    public required string Names { get; init; }
    public required string Surnames { get; init; }
}