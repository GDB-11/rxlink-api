namespace Application.Core.DTOs.Lookup.Response;

public sealed class SpecialtyPricingLookupItemResponse
{
    public required Guid Code { get; init; }
    public required decimal PriceInPerson { get; init; }
    public required decimal PriceVirtual { get; init; }
}
