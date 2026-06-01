namespace Infrastructure.Core.Services.Users;

/// <summary>
/// SQL used exclusively by <see cref="UserRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// Catalog integer IDs are never returned to the caller; only public UUIDs (codes) are projected.
/// Person data is managed exclusively through /api/person; user operations only link to an existing Person.
/// </summary>
internal static class UserRepositorySql
{
    /// <summary>
    /// Returns one page of users (not soft-deleted) ordered by surname then name.
    /// <c>COUNT(*) OVER()</c> embeds the total matching count in every row.
    /// The LATERAL subquery picks the most recent PersonDocument per person (LEFT JOIN — person may have none).
    /// Optional <c>@Search</c> filters by names, surnames, username, or email (case-insensitive).
    /// </summary>
    internal const string GetPage = """
        SELECT
            u."UserCode",
            p."PersonCode",
            p."Names",
            p."Surnames",
            p."BirthDate",
            s."SexCode",
            s."Name"                AS "SexName",
            p."Phone",
            p."AlternativePhone",
            p."Email"               AS "PersonEmail",
            p."Address",
            p."EmergencyContactName",
            p."EmergencyContactPhone",
            dt."DocumentTypeCode",
            dt."Name"               AS "DocumentTypeName",
            pd."Number"             AS "DocumentNumber",
            pd."IssueDate"          AS "DocumentIssueDate",
            pd."ExpirationDate"     AS "DocumentExpirationDate",
            r."RoleCode",
            r."Name"                AS "RoleName",
            sp."SpecialtyCode",
            sp."Name"               AS "SpecialtyName",
            u."Username",
            u."Email",
            u."LicenseNumber",
            u."IsActive",
            u."CreatedAt",
            COUNT(*) OVER ()        AS "TotalCount"
        FROM "User" u
        INNER JOIN "Person" p ON p."PersonId" = u."PersonId"
        INNER JOIN "Sex" s ON s."SexId" = p."SexId"
        LEFT JOIN LATERAL (
            SELECT *
            FROM "PersonDocument"
            WHERE "PersonId" = p."PersonId"
            ORDER BY "PersonDocumentId" DESC
            LIMIT 1
        ) pd ON TRUE
        LEFT JOIN "DocumentType" dt ON dt."DocumentTypeId" = pd."DocumentTypeId"
        INNER JOIN "Role" r ON r."RoleId" = u."RoleId"
        LEFT JOIN "Specialty" sp ON sp."SpecialtyId" = u."SpecialtyId"
        WHERE (@Search IS NULL OR
               p."Names"    ILIKE '%' || @Search || '%' OR
               p."Surnames" ILIKE '%' || @Search || '%' OR
               u."Username" ILIKE '%' || @Search || '%' OR
               u."Email"    ILIKE '%' || @Search || '%')
        ORDER BY p."Surnames", p."Names"
        LIMIT @Limit OFFSET @Offset
        """;

    /// <summary>
    /// Links an existing Person (identified by PersonCode) to a new User account.
    /// Resolves catalog codes to internal IDs in a <c>refs</c> CTE, then inserts the User row.
    /// Returns no rows when PersonCode is not found or any catalog code (role) is invalid.
    /// </summary>
    internal const string Insert = """
        WITH refs AS (
            SELECT
                p."PersonId",
                p."PersonCode",
                r."RoleId",
                r."RoleCode",
                r."Name"   AS "RoleName",
                sp."SpecialtyId",
                sp."SpecialtyCode",
                sp."Name"  AS "SpecialtyName"
            FROM "Person" p
            CROSS JOIN "Role" r
            LEFT JOIN "Specialty" sp
                ON sp."SpecialtyCode" = @SpecialtyCode
               AND sp."IsActive"      = TRUE
            WHERE p."PersonCode" = @PersonCode
              AND r."Name"       = @RoleName
              AND r."IsActive"   = TRUE
        ),
        new_user AS (
            INSERT INTO "User" (
                "PersonId", "RoleId", "SpecialtyId",
                "Username", "Email", "PasswordHash", "LicenseNumber"
            )
            SELECT
                refs."PersonId",
                refs."RoleId",
                refs."SpecialtyId",
                @Username,
                @Email,
                @PasswordHash,
                @LicenseNumber
            FROM refs
            RETURNING *
        )
        SELECT
            nu."UserCode",
            refs."PersonCode",
            p."Names",
            p."Surnames",
            p."BirthDate",
            s."SexCode",
            s."Name"                AS "SexName",
            p."Phone",
            p."AlternativePhone",
            p."Email"               AS "PersonEmail",
            p."Address",
            p."EmergencyContactName",
            p."EmergencyContactPhone",
            dt."DocumentTypeCode",
            dt."Name"               AS "DocumentTypeName",
            pd."Number"             AS "DocumentNumber",
            pd."IssueDate"          AS "DocumentIssueDate",
            pd."ExpirationDate"     AS "DocumentExpirationDate",
            refs."RoleCode",
            refs."RoleName",
            refs."SpecialtyCode",
            refs."SpecialtyName",
            nu."Username",
            nu."Email",
            nu."LicenseNumber",
            nu."IsActive",
            nu."CreatedAt",
            0                       AS "TotalCount"
        FROM new_user nu
        INNER JOIN refs ON refs."PersonId" = nu."PersonId"
        INNER JOIN "Person" p ON p."PersonId" = nu."PersonId"
        INNER JOIN "Sex" s ON s."SexId" = p."SexId"
        LEFT JOIN LATERAL (
            SELECT *
            FROM "PersonDocument"
            WHERE "PersonId" = p."PersonId"
            ORDER BY "PersonDocumentId" DESC
            LIMIT 1
        ) pd ON TRUE
        LEFT JOIN "DocumentType" dt ON dt."DocumentTypeId" = pd."DocumentTypeId"
        """;

    /// <summary>
    /// Updates only the account fields of an existing User. Person data is never touched.
    /// Resolves catalog codes via <c>refs</c>, then updates the User row.
    /// Returns no rows when the user is not found/deleted or any catalog code is invalid.
    /// </summary>
    internal const string Update = """
        WITH refs AS (
            SELECT
                r."RoleId",
                r."RoleCode",
                r."Name"   AS "RoleName",
                sp."SpecialtyId",
                sp."SpecialtyCode",
                sp."Name"  AS "SpecialtyName"
            FROM "Role" r
            LEFT JOIN "Specialty" sp
                ON sp."SpecialtyCode" = @SpecialtyCode
               AND sp."IsActive"      = TRUE
            WHERE r."Name"    = @RoleName
              AND r."IsActive" = TRUE
        ),
        upd_user AS (
            UPDATE "User" u
            SET
                "RoleId"        = refs."RoleId",
                "SpecialtyId"   = refs."SpecialtyId",
                "Username"      = @Username,
                "Email"         = @Email,
                "LicenseNumber" = @LicenseNumber
            FROM refs
            WHERE u."UserCode"  = @Code
              AND u."IsActive"  = TRUE
              AND u."DeletedAt" IS NULL
            RETURNING u."PersonId", u."UserCode", u."Username", u."Email",
                      u."LicenseNumber", u."IsActive", u."CreatedAt",
                      refs."RoleCode", refs."RoleName",
                      refs."SpecialtyCode", refs."SpecialtyName"
        )
        SELECT
            uu."UserCode",
            p."PersonCode",
            p."Names",
            p."Surnames",
            p."BirthDate",
            s."SexCode",
            s."Name"                AS "SexName",
            p."Phone",
            p."AlternativePhone",
            p."Email"               AS "PersonEmail",
            p."Address",
            p."EmergencyContactName",
            p."EmergencyContactPhone",
            dt."DocumentTypeCode",
            dt."Name"               AS "DocumentTypeName",
            pd."Number"             AS "DocumentNumber",
            pd."IssueDate"          AS "DocumentIssueDate",
            pd."ExpirationDate"     AS "DocumentExpirationDate",
            uu."RoleCode",
            uu."RoleName",
            uu."SpecialtyCode",
            uu."SpecialtyName",
            uu."Username",
            uu."Email",
            uu."LicenseNumber",
            uu."IsActive",
            uu."CreatedAt",
            0                       AS "TotalCount"
        FROM upd_user uu
        INNER JOIN "Person" p ON p."PersonId" = uu."PersonId"
        INNER JOIN "Sex" s ON s."SexId" = p."SexId"
        LEFT JOIN LATERAL (
            SELECT *
            FROM "PersonDocument"
            WHERE "PersonId" = p."PersonId"
            ORDER BY "PersonDocumentId" DESC
            LIMIT 1
        ) pd ON TRUE
        LEFT JOIN "DocumentType" dt ON dt."DocumentTypeId" = pd."DocumentTypeId"
        """;

    /// <summary>
    /// Soft-deletes an active user.
    /// <c>DeletedBy</c> is resolved from the caller's <c>UserCode</c> via a subquery.
    /// Affects 0 rows when the code does not match an active, non-deleted record.
    /// </summary>
    internal const string Deactivate = """
        UPDATE "User"
        SET
            "IsActive"  = FALSE,
            "DeletedAt" = NOW(),
            "DeletedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode AND "IsActive" = TRUE)
        WHERE "UserCode"  = @Code
          AND "IsActive"  = TRUE
          AND "DeletedAt" IS NULL
        """;

    /// <summary>
    /// Reactivates an active user.
    /// Affects 0 rows when the code does not match an active, non-deleted record.
    /// </summary>
    internal const string Activate = """
        UPDATE "User"
        SET
            "IsActive"  = TRUE,
            "DeletedAt" = NULL,
            "DeletedBy" = NULL
        WHERE "UserCode"  = @Code
          AND "IsActive"  = FALSE
          AND "DeletedAt" IS NOT NULL
        """;
}
