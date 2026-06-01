namespace Application.Core.DTOs.Role.Request;

public sealed record RolePageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}