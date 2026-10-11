namespace Infrastructure.Core.Services.Specialty;

/// <summary>
/// SQL used exclusively by <see cref="SpecialtyRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class SpecialtyRepositorySql
{
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