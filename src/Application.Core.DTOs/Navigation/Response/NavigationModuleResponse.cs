namespace Application.Core.DTOs.Navigation.Response;

public sealed record NavigationModuleResponse
{
    public required Guid ModuleCode { get; init; }
    public required string Label { get; init; }
    public required string Icon { get; init; }
    public required int Order { get; init; }
    public required IReadOnlyList<NavigationItemResponse> Items { get; init; }
}