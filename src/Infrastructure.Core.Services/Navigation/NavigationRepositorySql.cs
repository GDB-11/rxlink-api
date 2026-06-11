namespace Infrastructure.Core.Services.Navigation;

/// <summary>
/// SQL used exclusively by <see cref="NavigationRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class NavigationRepositorySql
{
    /// <summary>
    /// Returns one row per accessible (module, item) pair for the given role name,
    /// ordered by module position then item position within each module.
    ///
    /// Strategy:
    ///   • Filter <c>RoleNavigationAccess</c> rows where <c>NavigationItemId IS NULL</c>
    ///     to get the topbar modules granted to the role.
    ///   • LEFT JOIN back to <c>RoleNavigationAccess</c> (item rows) + <c>NavigationItem</c>
    ///     to attach each module's sidebar items.
    ///   • Modules with no items appear once with NULL item columns.
    /// </summary>
    internal const string GetRowsByRole = """
                                          SELECT
                                              nm."NavigationModuleCode" AS "ModuleCode",
                                              nm."Label"               AS "ModuleLabel",
                                              nm."Icon"                AS "ModuleIcon",
                                              rna_m."DisplayOrder"     AS "ModuleOrder",
                                              ni."NavigationItemCode"  AS "ItemCode",
                                              ni."Label"               AS "ItemLabel",
                                              ni."Icon"                AS "ItemIcon",
                                              ni."Path"                AS "ItemPath",
                                              rna_i."DisplayOrder"     AS "ItemOrder"
                                          FROM "RoleNavigationAccess" rna_m
                                          INNER JOIN "NavigationModule" nm
                                              ON  nm."NavigationModuleId" = rna_m."NavigationModuleId"
                                              AND nm."IsActive" = TRUE
                                          INNER JOIN "Role" r
                                              ON  r."RoleId" = rna_m."RoleId"
                                              AND r."Name"   = @RoleName
                                          LEFT JOIN "RoleNavigationAccess" rna_i
                                              ON  rna_i."RoleId"             = rna_m."RoleId"
                                              AND rna_i."NavigationModuleId" = rna_m."NavigationModuleId"
                                              AND rna_i."NavigationItemId"   IS NOT NULL
                                          LEFT JOIN "NavigationItem" ni
                                              ON  ni."NavigationItemId" = rna_i."NavigationItemId"
                                              AND ni."IsActive" = TRUE
                                          WHERE rna_m."NavigationItemId" IS NULL
                                          ORDER BY rna_m."DisplayOrder", rna_i."DisplayOrder"
                                          """;
}