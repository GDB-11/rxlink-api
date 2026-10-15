namespace Infrastructure.Core.Models.Specialty;

/// <summary>Active doctors per specialty code; names come from the catalog snapshot.</summary>
public sealed class SpecialtyDoctorCountRow
{
    public required Guid SpecialtyCode { get; init; }
    public required int DoctorCount { get; init; }
}
