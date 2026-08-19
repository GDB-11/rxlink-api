namespace Application.Core.DTOs.Lookup.Response;

public sealed record AppointmentLookupsResponse
{
    public required IReadOnlyList<GuidLookupItemResponse> ConsultationTypes { get; init; }
    public required IReadOnlyList<InsuranceLookupItemResponse> Insurances { get; init; }
    public required IReadOnlyList<SpecialtyPricingLookupItemResponse> Specialties { get; init; }
}
