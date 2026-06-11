namespace Infrastructure.Core.Models.Specialty;

public sealed class DoctorSummaryRow
{
    public required string SpecialtyName { get; init; }
    public Guid? UserCode { get; init; }
    public string? Names { get; init; }
    public string? Surnames { get; init; }
    public string? LicenseNumber { get; init; }
}