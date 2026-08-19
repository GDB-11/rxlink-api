namespace Infrastructure.Core.Models.Lookup;

public sealed class SpecialtyPricingLookupRow
{
    public required Guid Code { get; init; }
    public required decimal PriceInPerson { get; init; }
    public required decimal PriceVirtual { get; init; }
}
