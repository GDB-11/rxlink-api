namespace Application.Core.DTOs.Specialty.Response;

public sealed record DoctorSummaryResponse
{
    public required Guid UserCode { get; init; }
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public string? LicenseNumber { get; init; }
    public required string SpecialtyName { get; init; }
}
