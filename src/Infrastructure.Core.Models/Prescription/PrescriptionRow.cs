namespace Infrastructure.Core.Models.Prescription;

/// <summary>
/// Flat row returned by prescription queries.
/// DetailsJson is a JSON array of detail objects; empty array when no details exist.
/// </summary>
public sealed class PrescriptionRow
{
    public required Guid PrescriptionCode { get; init; }
    public required Guid DiagnosticCode { get; init; }
    public required string DiagnosticDescription { get; init; }
    public required Guid PatientCode { get; init; }
    public required Guid StatusCode { get; init; }
    public required string StatusName { get; init; }
    public string? Notes { get; init; }
    public required DateOnly ValidUntil { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string DetailsJson { get; init; }
}
