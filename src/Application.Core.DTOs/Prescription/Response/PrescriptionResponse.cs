namespace Application.Core.DTOs.Prescription.Response;

public sealed class PrescriptionResponse
{
    public required Guid PrescriptionCode { get; init; }
    public required Guid DiagnosticCode { get; init; }
    public required string DiagnosticDescription { get; init; }
    public required Guid PatientCode { get; init; }
    public required string StatusName { get; init; }
    public required Guid StatusCode { get; init; }
    public string? Notes { get; init; }
    public required DateOnly ValidUntil { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required IReadOnlyList<PrescriptionDetailResponse> Details { get; init; }
}