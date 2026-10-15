namespace Application.Core.DTOs.Disclosure.Response;

public sealed class DisclosureResponse
{
    public required Guid DisclosureCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required DateTimeOffset DisclosedAt { get; init; }
    public required string RecipientName { get; init; }
    public required string RecipientType { get; init; }
    public required string Purpose { get; init; }
    public required string Description { get; init; }
    public required string RecordedByName { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
}

public sealed class DisclosurePageResponse
{
    public required List<DisclosureResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
