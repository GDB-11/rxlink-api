namespace Infrastructure.Core.Models.PatientAuth;

public sealed class NewPatientData
{
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required Guid SexCode { get; init; }
    public required string Phone { get; init; }
    public string? AlternativePhone { get; init; }
    public required string Email { get; init; }
    public string? Address { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public required Guid DocumentTypeCode { get; init; }
    public required string DocumentNumber { get; init; }
}
