namespace Infrastructure.Core.Models.Disclosure;

public sealed class DisclosureRow
{
    public required Guid DisclosureCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required DateTimeOffset DisclosedAt { get; init; }
    public required string RecipientName { get; init; }
    public required string RecipientType { get; init; }
    public required string Purpose { get; init; }
    public required string Description { get; init; }
    public required string RecordedByName { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public long TotalCount { get; init; }
}
