namespace Infrastructure.Core.Services.Specialty;

/// <summary>
/// SQL used exclusively by <see cref="SpecialtyRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class SpecialtyRepositorySql
{
    /// <summary>
    /// Returns one page of specialties ordered by name.
    /// <c>COUNT(*) OVER()</c> embeds the total matching count in every row,
    /// avoiding a separate count query.
    /// Optional <c>@Search</c> filters by name (case-insensitive).
    /// </summary>
    internal const string GetPage = """
        SELECT
            s."SpecialtyCode",
            s."Name",
            s."IsActive",
            COUNT(*) OVER () AS "TotalCount"
        FROM "Specialty" s
        WHERE (@Search IS NULL OR s."Name" ILIKE '%' || @Search || '%')
        ORDER BY s."Name"
        LIMIT @Limit OFFSET @Offset
        """;

    /// <summary>
    /// Inserts a new specialty and returns the full row via CTE.
    /// </summary>
    internal const string Insert = """
        WITH ins AS (
            INSERT INTO "Specialty" ("Name")
            VALUES (@Name)
            RETURNING *
        )
        SELECT
            ins."SpecialtyCode",
            ins."Name",
            ins."IsActive",
            0 AS "TotalCount"
        FROM ins
        """;

    /// <summary>
    /// Updates an active specialty by code and returns the updated full row via CTE.
    /// Returns no rows when the code does not match an active record.
    /// </summary>
    internal const string Update = """
        WITH upd AS (
            UPDATE "Specialty"
            SET "Name" = @Name
            WHERE "SpecialtyCode" = @Code
              AND "IsActive" = TRUE
            RETURNING *
        )
        SELECT
            upd."SpecialtyCode",
            upd."Name",
            upd."IsActive",
            0 AS "TotalCount"
        FROM upd
        """;

    /// <summary>
    /// Soft-deletes an active specialty.
    /// Affects 0 rows when the code does not match an active record.
    /// </summary>
    internal const string Deactivate = """
        UPDATE "Specialty"
        SET "IsActive" = FALSE
        WHERE "SpecialtyCode" = @Code
          AND "IsActive" = TRUE
        """;

    /// <summary>
    /// Reactivates a previously deactivated specialty.
    /// Affects 0 rows when the code does not match an inactive record.
    /// </summary>
    internal const string Activate = """
        UPDATE "Specialty"
        SET "IsActive" = TRUE
        WHERE "SpecialtyCode" = @Code
          AND "IsActive" = FALSE
        """;
}
