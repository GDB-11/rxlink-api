namespace Infrastructure.Core.Services.Allergy;

/// <summary>
/// SQL used exclusively by <see cref="AllergyRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class AllergyRepositorySql
{
    /// <summary>
    /// Returns one page of allergies ordered by name.
    /// <c>COUNT(*) OVER()</c> embeds the total matching count in every row,
    /// avoiding a separate count query.
    /// Optional <c>@Search</c> filters by name (case-insensitive).
    /// </summary>
    internal const string GetPage = """
        SELECT
            a."AllergyCode",
            a."Name",
            a."Description",
            a."IsActive",
            COUNT(*) OVER () AS "TotalCount"
        FROM "Allergy" a
        WHERE (@Search IS NULL OR a."Name" ILIKE '%' || @Search || '%')
        ORDER BY a."Name"
        LIMIT @Limit OFFSET @Offset
        """;

    /// <summary>
    /// Inserts a new allergy and returns the full row via CTE.
    /// </summary>
    internal const string Insert = """
        WITH ins AS (
            INSERT INTO "Allergy" ("Name", "Description")
            VALUES (@Name, @Description)
            RETURNING *
        )
        SELECT
            ins."AllergyCode",
            ins."Name",
            ins."Description",
            ins."IsActive",
            0 AS "TotalCount"
        FROM ins
        """;

    /// <summary>
    /// Updates an active allergy by code and returns the updated full row via CTE.
    /// Returns no rows when the code does not match an active record.
    /// </summary>
    internal const string Update = """
        WITH upd AS (
            UPDATE "Allergy"
            SET
                "Name"        = @Name,
                "Description" = @Description
            WHERE "AllergyCode" = @Code
              AND "IsActive" = TRUE
            RETURNING *
        )
        SELECT
            upd."AllergyCode",
            upd."Name",
            upd."Description",
            upd."IsActive",
            0 AS "TotalCount"
        FROM upd
        """;

    /// <summary>
    /// Soft-deletes an active allergy.
    /// Affects 0 rows when the code does not match an active record.
    /// </summary>
    internal const string Deactivate = """
        UPDATE "Allergy"
        SET "IsActive" = FALSE
        WHERE "AllergyCode" = @Code
          AND "IsActive" = TRUE
        """;

    /// <summary>
    /// Reactivates a previously deactivated allergy.
    /// Affects 0 rows when the code does not match an inactive record.
    /// </summary>
    internal const string Activate = """
        UPDATE "Allergy"
        SET "IsActive" = TRUE
        WHERE "AllergyCode" = @Code
          AND "IsActive" = FALSE
        """;
}
