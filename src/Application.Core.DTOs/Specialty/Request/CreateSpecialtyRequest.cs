namespace Application.Core.DTOs.Specialty.Request;

public sealed record CreateSpecialtyRequest
{
    public required string Name { get; init; }
    
}