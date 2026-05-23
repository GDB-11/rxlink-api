namespace Infrastructure.Core.Models.Specialty;

public sealed class SpecialtyRow
{
    public required Guid SpecialtyCode { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required long TotalCount { get; init; }
}