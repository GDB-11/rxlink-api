namespace Infrastructure.Core.Models.Specialty;

public sealed class DoctorSummaryRow
{
    public required Guid UserCode { get; init; }
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public string? LicenseNumber { get; init; }
}
