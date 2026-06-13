namespace Application.Core.DTOs.Prescription.Response;

public sealed class PrescriptionDetailResponse
{
    public required Guid PrescriptionDetailCode { get; init; }
    public required Guid MedicationCode { get; init; }
    public required string MedicationName { get; init; }
    public required string Dose { get; init; }
    public required Guid AdministrationRouteCode { get; init; }
    public required string AdministrationRouteName { get; init; }
    public required Guid FrequencyCode { get; init; }
    public required string FrequencyDescription { get; init; }
    public required int DurationDays { get; init; }
    public string? Instructions { get; init; }
}