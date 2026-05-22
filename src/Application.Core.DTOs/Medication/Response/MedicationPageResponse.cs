namespace Application.Core.DTOs.Medication.Response;

public sealed class MedicationPageResponse
{
    public required IReadOnlyList<MedicationResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
