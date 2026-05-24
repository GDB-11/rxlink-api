namespace Application.Core.DTOs.User.Response;

public sealed class UserPageResponse
{
    public required IReadOnlyList<UserResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
