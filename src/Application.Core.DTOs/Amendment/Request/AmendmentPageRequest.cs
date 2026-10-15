namespace Application.Core.DTOs.Amendment.Request;

public sealed record AmendmentPageRequest
{
    /// <summary>Optional filter: <c>Pendiente</c>, <c>Aceptada</c> or <c>Denegada</c>.</summary>
    public string? Status { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
