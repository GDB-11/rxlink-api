namespace Infrastructure.Core.Models.Navigation;

/// <summary>
/// Flat row returned by the navigation query.
/// Each row represents one (module, item) pair; modules with N items produce N rows.
/// Item columns are <c>null</c> when a module has no accessible items.
/// </summary>
public sealed class NavigationRow
{
    public required Guid ModuleCode { get; init; }
    public required string ModuleLabel { get; init; }
    public required string ModuleIcon { get; init; }
    public required int ModuleOrder { get; init; }

    // Nullable — LEFT JOIN to RoleNavigationAccess item rows + NavigationItem
    public Guid? ItemCode { get; init; }
    public string? ItemLabel { get; init; }
    public string? ItemIcon { get; init; }
    public string? ItemPath { get; init; }
    public int? ItemOrder { get; init; }
}