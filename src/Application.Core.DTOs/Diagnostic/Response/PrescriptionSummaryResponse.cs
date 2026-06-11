namespace Application.Core.DTOs.Diagnostic.Response;

public sealed class PrescriptionSummaryResponse
{
    public required Guid PrescriptionCode { get; init; }
    public required Guid StatusCode { get; init; }
    public required string StatusName { get; init; }
    public required DateOnly ValidUntil { get; init; }
    public required int DetailCount { get; init; }
}