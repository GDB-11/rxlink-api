namespace Application.Core.DTOs.Diagnostic.Response;

public sealed class DiagnosticPageResponse
{
    public required List<DiagnosticResponse> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}