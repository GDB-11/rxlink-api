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
                                        s."PriceInPerson",
                                        s."PriceVirtual",
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
                                       INSERT INTO "Specialty" ("Name", "PriceInPerson", "PriceVirtual")
                                       VALUES (@Name, @PriceInPerson, @PriceVirtual)
                                       RETURNING *
                                   )
                                   SELECT
                                       ins."SpecialtyCode",
                                       ins."Name",
                                       ins."PriceInPerson",
                                       ins."PriceVirtual",
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
                                       SET "Name" = @Name, "PriceInPerson" = @PriceInPerson, "PriceVirtual" = @PriceVirtual
                                       WHERE "SpecialtyCode" = @Code
                                         AND "IsActive" = TRUE
                                       RETURNING *
                                   )
                                   SELECT
                                       upd."SpecialtyCode",
                                       upd."Name",
                                       upd."PriceInPerson",
                                       upd."PriceVirtual",
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

    /// <summary>
    /// Returns all active specialties with the count of active, non-deleted doctors assigned to each.
    /// </summary>
    internal const string GetAllActiveWithDoctorCount = """
                                                        SELECT
                                                            s."SpecialtyCode",
                                                            s."Name",
                                                            COUNT(u."UserId")::integer AS "DoctorCount"
                                                        FROM "Specialty" s
                                                        LEFT JOIN "User" u ON u."SpecialtyId" = s."SpecialtyId"
                                                                           AND u."IsActive"    = TRUE
                                                                           AND u."DeletedAt"   IS NULL
                                                                           AND u."RoleId"      = (SELECT "RoleId" FROM "Role" WHERE "Name" = 'Doctor')
                                                        WHERE s."IsActive" = TRUE
                                                        GROUP BY s."SpecialtyId", s."SpecialtyCode", s."Name"
                                                        ORDER BY s."Name"
                                                        """;

    /// <summary>
    /// Returns all active, non-deleted doctors for a given active specialty.
    /// Returns 0 rows when the specialty code does not match an active record.
    /// Returns 1 row with null UserCode when the specialty exists but has no active doctors.
    /// </summary>
    internal const string GetDoctorsBySpecialtyCode = """
                                                      SELECT
                                                          s."Name"           AS "SpecialtyName",
                                                          u."UserCode",
                                                          p."Names",
                                                          p."Surnames",
                                                          u."LicenseNumber"
                                                      FROM "Specialty" s
                                                      LEFT JOIN "User" u   ON u."SpecialtyId" = s."SpecialtyId"
                                                                           AND u."IsActive"    = TRUE
                                                                           AND u."DeletedAt"   IS NULL
                                                                           AND u."RoleId"      = (SELECT "RoleId" FROM "Role" WHERE "Name" = 'Doctor')
                                                      LEFT JOIN "Person" p ON p."PersonId" = u."PersonId"
                                                      WHERE s."SpecialtyCode" = @SpecialtyCode
                                                        AND s."IsActive"      = TRUE
                                                        AND (
                                                            @Search IS NULL
                                                            OR p."Names"    ILIKE '%' || @Search || '%'
                                                            OR p."Surnames" ILIKE '%' || @Search || '%'
                                                        )
                                                      ORDER BY p."Surnames", p."Names"
                                                      """;
}