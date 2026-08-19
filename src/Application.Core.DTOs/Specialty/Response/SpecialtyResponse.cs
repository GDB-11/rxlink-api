namespace Application.Core.DTOs.Specialty.Response;

public sealed class SpecialtyResponse
{
    public required Guid SpecialtyCode { get; init; }
    public required string Name { get; init; }
    public required decimal PriceInPerson { get; init; }
    public required decimal PriceVirtual { get; init; }
    public required bool IsActive { get; init; }
}