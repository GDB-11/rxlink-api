namespace Application.Core.DTOs.Clinic.Response;

public sealed class ClinicResponse
{
    public required Guid ClinicCode { get; init; }
    public required string Name { get; init; }
    public string? Address { get; init; }
    public string? District { get; init; }
    public string? Province { get; init; }
    public string? Department { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public string? FacilityRegistrationCode { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
