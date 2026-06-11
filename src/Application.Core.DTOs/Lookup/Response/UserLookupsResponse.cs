namespace Application.Core.DTOs.Lookup.Response;

public sealed class UserLookupsResponse
{
    public required IReadOnlyList<GuidLookupItemResponse> Sexes { get; init; }
    public required IReadOnlyList<GuidLookupItemResponse> DocumentTypes { get; init; }
    public required IReadOnlyList<GuidLookupItemResponse> Roles { get; init; }
    public required IReadOnlyList<GuidLookupItemResponse> Specialties { get; init; }
}