namespace Infrastructure.Core.Models.Lookup;

public sealed class GuidLookupRow
{
    public required Guid Code { get; init; }
    public required string Name { get; init; }
}
