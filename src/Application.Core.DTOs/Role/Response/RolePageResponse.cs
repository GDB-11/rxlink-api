namespace Application.Core.DTOs.Role.Response;

public sealed class RolePageResponse
{
    public required IReadOnlyList<RoleResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}