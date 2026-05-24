namespace Application.Core.DTOs.Lookup.Response;

public sealed class GuidLookupItemResponse
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
}
