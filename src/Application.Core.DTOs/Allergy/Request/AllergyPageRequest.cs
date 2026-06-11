namespace Application.Core.DTOs.Allergy.Request;

public sealed record AllergyPageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}