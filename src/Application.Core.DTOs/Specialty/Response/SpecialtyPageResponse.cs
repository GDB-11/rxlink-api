namespace Application.Core.DTOs.Specialty.Response;

public sealed class SpecialtyPageResponse
{
    public required IReadOnlyList<SpecialtyResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}