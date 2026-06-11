namespace Infrastructure.Core.Services.Role;

internal static class RoleRepositorySql
{
    /// <summary>
    /// Returns one page of roles ordered by name.
    /// <c>COUNT(*) OVER()</c> embeds the total matching count in every row.
    /// Optional <c>@Search</c> filters by name (case-insensitive).
    /// </summary>
    internal const string GetPage = """
                                    SELECT
                                        r."RoleCode",
                                        r."Name",
                                        r."Description",
                                        r."IsActive",
                                        r."CreatedAt",
                                        COUNT(*) OVER () AS "TotalCount"
                                    FROM "Role" r
                                    WHERE (@Search IS NULL OR r."Name" ILIKE '%' || @Search || '%')
                                    ORDER BY r."Name"
                                    LIMIT @Limit OFFSET @Offset
                                    """;

    /// <summary>
    /// Inserts a new role after checking that no active role with the same name exists.
    /// Returns no rows when the name is already taken by an active role.
    /// </summary>
    internal const string Insert = """
                                   WITH dup AS (
                                       SELECT 1 FROM "Role"
                                       WHERE "Name" = @Name AND "IsActive" = TRUE
                                   ),
                                   ins AS (
                                       INSERT INTO "Role" ("Name", "Description")
                                       SELECT @Name, @Description
                                       WHERE NOT EXISTS (SELECT 1 FROM dup)
                                       RETURNING *
                                   )
                                   SELECT
                                       ins."RoleCode",
                                       ins."Name",
                                       ins."Description",
                                       ins."IsActive",
                                       ins."CreatedAt",
                                       0 AS "TotalCount"
                                   FROM ins
                                   """;

    /// <summary>
    /// Updates an active role by code. Returns no rows when the code does not match
    /// an active, non-deleted record.
    /// </summary>
    internal const string Update = """
                                   WITH upd AS (
                                       UPDATE "Role"
                                       SET "Name" = @Name, "Description" = @Description
                                       WHERE "RoleCode" = @Code
                                         AND "IsActive" = TRUE
                                         AND "DeletedAt" IS NULL
                                       RETURNING *
                                   )
                                   SELECT
                                       upd."RoleCode",
                                       upd."Name",
                                       upd."Description",
                                       upd."IsActive",
                                       upd."CreatedAt",
                                       0 AS "TotalCount"
                                   FROM upd
                                   """;

    /// <summary>
    /// Soft-deletes an active role. <c>DeletedBy</c> is resolved from the caller's
    /// <c>UserCode</c> via a subquery. Affects 0 rows when the code does not match
    /// an active, non-deleted record.
    /// </summary>
    internal const string Deactivate = """
                                       UPDATE "Role"
                                       SET
                                           "IsActive"   = FALSE,
                                           "DeletedAt"  = NOW(),
                                           "DeletedBy"  = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE)
                                       WHERE "RoleCode" = @Code
                                         AND "IsActive" = TRUE
                                         AND "DeletedAt" IS NULL
                                       """;

    /// <summary>
    /// Reactivates a previously deactivated role. Clears audit columns.
    /// Affects 0 rows when the code does not match an inactive, deleted record.
    /// </summary>
    internal const string Activate = """
                                     UPDATE "Role"
                                     SET
                                         "IsActive"  = TRUE,
                                         "DeletedAt"  = NULL,
                                         "DeletedBy"  = NULL
                                     WHERE "RoleCode" = @Code
                                       AND "IsActive" = FALSE
                                       AND "DeletedAt" IS NOT NULL
                                     """;
}