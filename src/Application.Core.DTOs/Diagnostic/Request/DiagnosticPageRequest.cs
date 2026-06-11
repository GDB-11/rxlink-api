namespace Application.Core.DTOs.Diagnostic.Request;

public sealed record DiagnosticPageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}