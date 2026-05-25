namespace Application.Core.DTOs.Patient.Response;

public sealed class PatientPageResponse
{
    public required IReadOnlyList<PatientResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
