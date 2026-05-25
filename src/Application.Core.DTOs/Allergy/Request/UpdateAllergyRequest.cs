namespace Application.Core.DTOs.Allergy.Request;

public sealed record UpdateAllergyRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
