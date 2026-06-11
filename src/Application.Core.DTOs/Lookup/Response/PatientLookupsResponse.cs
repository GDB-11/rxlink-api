namespace Application.Core.DTOs.Lookup.Response;

public sealed class PatientLookupsResponse
{
    public required IReadOnlyList<GuidLookupItemResponse> AllergySeverities { get; init; }
}