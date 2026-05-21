namespace Application.Core.DTOs.Navigation.Response;

public sealed record NavigationResponse
{
    public required IReadOnlyList<NavigationModuleResponse> Modules { get; init; }
}