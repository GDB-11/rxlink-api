namespace Application.Core.DTOs.Person.Request;

public sealed record PersonPageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}
