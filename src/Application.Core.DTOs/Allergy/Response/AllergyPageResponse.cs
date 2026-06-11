namespace Application.Core.DTOs.Allergy.Response;

public sealed class AllergyPageResponse
{
    public required IReadOnlyList<AllergyResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}