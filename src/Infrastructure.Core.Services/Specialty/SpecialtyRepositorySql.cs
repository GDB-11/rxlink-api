namespace Infrastructure.Core.Services.Specialty;

/// <summary>
/// SQL used exclusively by <see cref="SpecialtyRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class SpecialtyRepositorySql
{
    /// <summary>
    /// Counts active, non-deleted doctors per specialty code. Aggregated by code only: specialty names and
    /// enablement come from the catalog snapshot, so this query never joins the (legacy) local catalog table.
    /// </summary>
    internal const string GetDoctorCountsBySpecialtyCode = """
                                                           SELECT
                                                               u."SpecialtyCode",
                                                               COUNT(u."UserId")::integer AS "DoctorCount"
                                                           FROM "User" u
                                                           WHERE u."SpecialtyCode" IS NOT NULL
                                                             AND u."IsActive"      = TRUE
                                                             AND u."DeletedAt"     IS NULL
                                                             AND u."RoleId"        = (SELECT "RoleId" FROM "Role" WHERE "Name" = 'Doctor')
                                                           GROUP BY u."SpecialtyCode"
                                                           """;

    /// <summary>
    /// Returns all active, non-deleted doctors for a given specialty code. The specialty itself is validated
    /// and named by the catalog snapshot, not by this query.
    /// </summary>
    internal const string GetDoctorsBySpecialtyCode = """
                                                      SELECT
                                                          u."UserCode",
                                                          p."Names",
                                                          p."Surnames",
                                                          u."LicenseNumber"
                                                      FROM "User" u
                                                      JOIN "Person" p ON p."PersonId" = u."PersonId"
                                                      WHERE u."SpecialtyCode" = @SpecialtyCode
                                                        AND u."IsActive"      = TRUE
                                                        AND u."DeletedAt"     IS NULL
                                                        AND u."RoleId"        = (SELECT "RoleId" FROM "Role" WHERE "Name" = 'Doctor')
                                                        AND (
                                                            @Search IS NULL
                                                            OR p."Names"    ILIKE '%' || @Search || '%'
                                                            OR p."Surnames" ILIKE '%' || @Search || '%'
                                                        )
                                                      ORDER BY p."Surnames", p."Names"
                                                      """;
}
