namespace Application.Core.DTOs.Specialty.Response;

public sealed record SpecialtyWithDoctorCountResponse
{
    public required Guid SpecialtyCode { get; init; }
    public required string Name { get; init; }
    public required int DoctorCount { get; init; }
}