namespace Application.Core.DTOs.Specialty.Request;

public sealed record UpdateSpecialtyRequest
{
    public required string Name { get; init; }
}