namespace Application.Core.DTOs.Lookup.Response;

public sealed record AppointmentLookupsResponse
{
    public required IReadOnlyList<GuidLookupItemResponse> ConsultationTypes { get; init; }
}
