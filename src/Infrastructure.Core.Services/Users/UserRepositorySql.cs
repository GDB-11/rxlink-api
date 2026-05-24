namespace Infrastructure.Core.Services.Users;

/// <summary>
/// SQL used exclusively by <see cref="UserRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// Catalog integer IDs are never returned to the caller; only public UUIDs (codes) are projected.
/// </summary>
internal static class UserRepositorySql
{
    /// <summary>
    /// Returns one page of users (not soft-deleted) ordered by surname then name.
    /// <c>COUNT(*) OVER()</c> embeds the total matching count in every row.
    /// The LATERAL subquery picks the most recent PersonDocument per person.
    /// Optional <c>@Search</c> filters by names, surnames, username, or email (case-insensitive).
    /// </summary>
    internal const string GetPage = """
        SELECT
            u."UserCode",
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
        INNER JOIN LATERAL (
            SELECT *
            FROM "PersonDocument"
            WHERE "PersonId" = p."PersonId"
            ORDER BY "PersonDocumentId" DESC
            LIMIT 1
        ) pd ON TRUE
        INNER JOIN "DocumentType" dt ON dt."DocumentTypeId" = pd."DocumentTypeId"
        INNER JOIN "Role" r ON r."RoleId" = u."RoleId"
        LEFT JOIN "Specialty" sp ON sp."SpecialtyId" = u."SpecialtyId"
        WHERE u."DeletedAt" IS NULL
          AND (@Search IS NULL OR
               p."Names"    ILIKE '%' || @Search || '%' OR
               p."Surnames" ILIKE '%' || @Search || '%' OR
               u."Username" ILIKE '%' || @Search || '%' OR
               u."Email"    ILIKE '%' || @Search || '%')
        ORDER BY p."Surnames", p."Names"
        LIMIT @Limit OFFSET @Offset
        """;

    /// <summary>
    /// Resolves all public catalog codes to internal IDs in a single <c>refs</c> CTE, then
    /// inserts Person, PersonDocument and User in a dependency chain that guarantees atomicity:
    /// if any code is invalid (<c>refs</c> returns 0 rows), nothing is inserted.
    /// Returns no rows when any required catalog code does not match an active record.
    /// </summary>
    internal const string Insert = """
        WITH refs AS (
            SELECT
                s."SexId",
                s."SexCode",
                s."Name"   AS "SexName",
                dt."DocumentTypeId",
                dt."DocumentTypeCode",
                dt."Name"  AS "DocumentTypeName",
                r."RoleId",
                r."RoleCode",
                r."Name"   AS "RoleName",
                sp."SpecialtyId",
                sp."SpecialtyCode",
                sp."Name"  AS "SpecialtyName"
            FROM "Sex" s
            CROSS JOIN "DocumentType" dt
            CROSS JOIN "Role" r
            LEFT JOIN "Specialty" sp
                ON sp."SpecialtyCode" = @SpecialtyCode
               AND sp."IsActive"      = TRUE
            WHERE s."SexCode"           = @SexCode
              AND dt."DocumentTypeCode" = @DocumentTypeCode
              AND dt."IsActive"         = TRUE
              AND r."Name"              = @RoleName
              AND r."IsActive"          = TRUE
        ),
        new_person AS (
            INSERT INTO "Person" (
                "Names", "Surnames", "BirthDate", "SexId",
                "Phone", "AlternativePhone", "Email", "Address",
                "EmergencyContactName", "EmergencyContactPhone"
            )
            SELECT
                @Names, @Surnames, @BirthDate, refs."SexId",
                @Phone, @AlternativePhone, @PersonEmail, @Address,
                @EmergencyContactName, @EmergencyContactPhone
            FROM refs
            RETURNING *
        ),
        new_document AS (
            INSERT INTO "PersonDocument" (
                "PersonId", "DocumentTypeId", "Number", "IssueDate", "ExpirationDate"
            )
            SELECT np."PersonId", refs."DocumentTypeId", @DocumentNumber, @DocumentIssueDate, @DocumentExpirationDate
            FROM new_person np, refs
            RETURNING *
        ),
        new_user AS (
            INSERT INTO "User" (
                "PersonId", "RoleId", "SpecialtyId",
                "Username", "Email", "PasswordHash", "LicenseNumber"
            )
            SELECT
                nd."PersonId",
                refs."RoleId",
                refs."SpecialtyId",
                @Username,
                @Email,
                @PasswordHash,
                @LicenseNumber
            FROM new_document nd, refs
            RETURNING *
        )
        SELECT
            nu."UserCode",
            np."Names",
            np."Surnames",
            np."BirthDate",
            refs."SexCode",
            refs."SexName",
            np."Phone",
            np."AlternativePhone",
            np."Email"             AS "PersonEmail",
            np."Address",
            np."EmergencyContactName",
            np."EmergencyContactPhone",
            refs."DocumentTypeCode",
            refs."DocumentTypeName",
            nd."Number"            AS "DocumentNumber",
            nd."IssueDate"         AS "DocumentIssueDate",
            nd."ExpirationDate"    AS "DocumentExpirationDate",
            refs."RoleCode",
            refs."RoleName",
            refs."SpecialtyCode",
            refs."SpecialtyName",
            nu."Username",
            nu."Email",
            nu."LicenseNumber",
            nu."IsActive",
            nu."CreatedAt",
            0                      AS "TotalCount"
        FROM new_user nu, new_person np, new_document nd, refs
        """;

    /// <summary>
    /// Resolves catalog codes first via <c>refs</c>, then updates Person, PersonDocument and User
    /// atomically via chained CTEs.
    /// Returns no rows when the user is not found/deleted or any catalog code is invalid.
    /// </summary>
    internal const string Update = """
        WITH refs AS (
            SELECT
                s."SexId",
                s."SexCode",
                s."Name"   AS "SexName",
                dt."DocumentTypeId",
                dt."DocumentTypeCode",
                dt."Name"  AS "DocumentTypeName",
                r."RoleId",
                r."RoleCode",
                r."Name"   AS "RoleName",
                sp."SpecialtyId",
                sp."SpecialtyCode",
                sp."Name"  AS "SpecialtyName"
            FROM "Sex" s
            CROSS JOIN "DocumentType" dt
            CROSS JOIN "Role" r
            LEFT JOIN "Specialty" sp
                ON sp."SpecialtyCode" = @SpecialtyCode
               AND sp."IsActive"      = TRUE
            WHERE s."SexCode"           = @SexCode
              AND dt."DocumentTypeCode" = @DocumentTypeCode
              AND dt."IsActive"         = TRUE
              AND r."Name"              = @RoleName
              AND r."IsActive"          = TRUE
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
        ),
        upd_person AS (
            UPDATE "Person" p
            SET
                "Names"                 = @Names,
                "Surnames"              = @Surnames,
                "BirthDate"             = @BirthDate,
                "SexId"                 = refs."SexId",
                "Phone"                 = @Phone,
                "AlternativePhone"      = @AlternativePhone,
                "Email"                 = @PersonEmail,
                "Address"               = @Address,
                "EmergencyContactName"  = @EmergencyContactName,
                "EmergencyContactPhone" = @EmergencyContactPhone
            FROM upd_user uu, refs
            WHERE p."PersonId" = uu."PersonId"
            RETURNING p.*, refs."SexCode", refs."SexName"
        ),
        upd_doc AS (
            UPDATE "PersonDocument" pd
            SET
                "DocumentTypeId"  = refs."DocumentTypeId",
                "Number"          = @DocumentNumber,
                "IssueDate"       = @DocumentIssueDate,
                "ExpirationDate"  = @DocumentExpirationDate
            FROM upd_person up, refs
            WHERE pd."PersonId" = up."PersonId"
            RETURNING pd.*, refs."DocumentTypeCode", refs."DocumentTypeName"
        )
        SELECT
            uu."UserCode",
            up."Names",
            up."Surnames",
            up."BirthDate",
            up."SexCode",
            up."SexName",
            up."Phone",
            up."AlternativePhone",
            up."Email"              AS "PersonEmail",
            up."Address",
            up."EmergencyContactName",
            up."EmergencyContactPhone",
            ud."DocumentTypeCode",
            ud."DocumentTypeName",
            ud."Number"             AS "DocumentNumber",
            ud."IssueDate"          AS "DocumentIssueDate",
            ud."ExpirationDate"     AS "DocumentExpirationDate",
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
        INNER JOIN upd_person up ON up."PersonId" = uu."PersonId"
        INNER JOIN upd_doc ud ON ud."PersonId" = up."PersonId"
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
          AND "IsActive"  = TRUE
          AND "DeletedAt" IS NULL
        """;
}
