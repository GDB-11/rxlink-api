namespace Application.Core.DTOs.Person.Response;

public sealed class PersonPageResponse
{
    public required IReadOnlyList<PersonResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
