namespace Application.Core.DTOs.User.Request;

public sealed record UserPageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}