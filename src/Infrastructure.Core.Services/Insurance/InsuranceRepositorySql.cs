namespace Infrastructure.Core.Services.Insurance;

/// <summary>
/// SQL used exclusively by <see cref="InsuranceRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class InsuranceRepositorySql
{
    /// <summary>
    /// Returns one page of insurances ordered by name.
    /// <c>COUNT(*) OVER()</c> embeds the total matching count in every row,
    /// avoiding a separate count query.
    /// Optional <c>@Search</c> filters by name (case-insensitive).
    /// </summary>
    internal const string GetPage = """
                                    SELECT
                                        i."InsuranceCode",
                                        i."Name",
                                        i."CoveragePercentage",
                                        i."IsActive",
                                        COUNT(*) OVER () AS "TotalCount"
                                    FROM "Insurance" i
                                    WHERE (@Search IS NULL OR i."Name" ILIKE '%' || @Search || '%')
                                    ORDER BY i."Name"
                                    LIMIT @Limit OFFSET @Offset
                                    """;

    /// <summary>
    /// Inserts a new insurance and returns the full row via CTE.
    /// </summary>
    internal const string Insert = """
                                   WITH ins AS (
                                       INSERT INTO "Insurance" ("Name", "CoveragePercentage")
                                       VALUES (@Name, @CoveragePercentage)
                                       RETURNING *
                                   )
                                   SELECT
                                       ins."InsuranceCode",
                                       ins."Name",
                                       ins."CoveragePercentage",
                                       ins."IsActive",
                                       0 AS "TotalCount"
                                   FROM ins
                                   """;

    /// <summary>
    /// Updates an active insurance by code and returns the updated full row via CTE.
    /// Returns no rows when the code does not match an active record.
    /// </summary>
    internal const string Update = """
                                   WITH upd AS (
                                       UPDATE "Insurance"
                                       SET "Name" = @Name, "CoveragePercentage" = @CoveragePercentage
                                       WHERE "InsuranceCode" = @Code
                                         AND "IsActive" = TRUE
                                       RETURNING *
                                   )
                                   SELECT
                                       upd."InsuranceCode",
                                       upd."Name",
                                       upd."CoveragePercentage",
                                       upd."IsActive",
                                       0 AS "TotalCount"
                                   FROM upd
                                   """;

    /// <summary>
    /// Soft-deletes an active insurance.
    /// Affects 0 rows when the code does not match an active record.
    /// </summary>
    internal const string Deactivate = """
                                       UPDATE "Insurance"
                                       SET "IsActive" = FALSE
                                       WHERE "InsuranceCode" = @Code
                                         AND "IsActive" = TRUE
                                       """;

    /// <summary>
    /// Reactivates a previously deactivated insurance.
    /// Affects 0 rows when the code does not match an inactive record.
    /// </summary>
    internal const string Activate = """
                                     UPDATE "Insurance"
                                     SET "IsActive" = TRUE
                                     WHERE "InsuranceCode" = @Code
                                       AND "IsActive" = FALSE
                                     """;
}
