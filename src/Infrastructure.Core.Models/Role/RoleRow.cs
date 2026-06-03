namespace Infrastructure.Core.Models.Role;

public sealed class RoleRow
{
    public required Guid RoleCode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required long TotalCount { get; init; }
}