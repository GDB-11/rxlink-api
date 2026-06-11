namespace Application.Core.DTOs.Allergy.Response;

public sealed class AllergyResponse
{
    public required Guid AllergyCode { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
}