namespace Infrastructure.Core.Models.Specialty;

public sealed class SpecialtyWithDoctorCountRow
{
    public required Guid SpecialtyCode { get; init; }
    public required string Name { get; init; }
    public required int DoctorCount { get; init; }
}