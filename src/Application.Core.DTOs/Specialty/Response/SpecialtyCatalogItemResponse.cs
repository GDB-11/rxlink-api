namespace Application.Core.DTOs.Specialty.Response;

/// <summary>A shared-catalog specialty merged with this partner's enablement and prices.</summary>
public sealed class SpecialtyCatalogItemResponse
{
    public required Guid SpecialtyCode { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsEnabled { get; init; }
    public required decimal PriceInPerson { get; init; }
    public required decimal PriceVirtual { get; init; }
}
