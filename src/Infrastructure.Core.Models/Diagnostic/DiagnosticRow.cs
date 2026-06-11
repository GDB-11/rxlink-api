namespace Infrastructure.Core.Models.Diagnostic;

/// <summary>
/// Flat row returned by diagnostic queries.
/// PrescriptionSummaryJson is a JSON object or NULL when no prescription exists.
/// TotalCount is populated only by paginated queries; single-row operations return 0.
/// </summary>
public sealed class DiagnosticRow
{
    public required Guid DiagnosticCode { get; init; }
    public required Guid PatientCode { get; init; }
    public required Guid StatusCode { get; init; }
    public required string StatusName { get; init; }
    public required string Description { get; init; }
    public required DateOnly DiagnosedAt { get; init; }
    public string? Notes { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string? PrescriptionSummaryJson { get; init; }
    public required long TotalCount { get; init; }
}