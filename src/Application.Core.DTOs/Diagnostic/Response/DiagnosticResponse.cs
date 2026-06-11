namespace Application.Core.DTOs.Diagnostic.Response;

public sealed class DiagnosticResponse
{
    public required Guid DiagnosticCode { get; init; }
    public required Guid AppointmentCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required Guid StatusCode { get; init; }
    public required string StatusName { get; init; }
    public required string Description { get; init; }
    public required DateOnly DiagnosedAt { get; init; }
    public string? Notes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public PrescriptionSummaryResponse? Prescription { get; init; }
}