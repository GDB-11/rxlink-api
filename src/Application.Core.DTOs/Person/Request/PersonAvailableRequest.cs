namespace Application.Core.DTOs.Person.Request;

public sealed record PersonAvailableRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public bool ExcludeLinkedUsers { get; init; } = false;
    public bool ExcludeLinkedPatients { get; init; } = false;
}
