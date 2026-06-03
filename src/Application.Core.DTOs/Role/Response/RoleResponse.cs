namespace Application.Core.DTOs.Role.Response;

public sealed class RoleResponse
{
    public required Guid RoleCode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
}