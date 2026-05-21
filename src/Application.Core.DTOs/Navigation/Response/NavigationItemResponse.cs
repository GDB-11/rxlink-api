namespace Application.Core.DTOs.Navigation.Response;

public sealed record NavigationItemResponse
{
    public required Guid ItemCode { get; init; }
    public required string Label { get; init; }
    public required string Icon { get; init; }
    public required string Path { get; init; }
    public required int Order { get; init; }
}