namespace Infrastructure.Core.Services.Medication;

/// <summary>
/// SQL used exclusively by <see cref="MedicationRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class MedicationRepositorySql
{
    /// <summary>
    /// Returns one page of medications ordered by generic name.
    /// <c>COUNT(*) OVER()</c> embeds the total matching count in every row,
    /// avoiding a separate count query.
    /// Optional <c>@Search</c> filters by generic or commercial name (case-insensitive).
    /// </summary>
    internal const string GetPage = """
        SELECT
            m."MedicationCode",
            m."PharmaceuticalFormId",
            pf."Name"              AS "PharmaceuticalFormName",
            m."AdministrationRouteId",
            ar."Name"              AS "AdministrationRouteName",
            m."GenericName",
            m."CommercialName",
            m."Concentration",
            m."IsActive",
            COUNT(*) OVER ()       AS "TotalCount"
        FROM "Medication" m
        INNER JOIN "PharmaceuticalForm"  pf ON pf."PharmaceuticalFormId"  = m."PharmaceuticalFormId"
        INNER JOIN "AdministrationRoute" ar ON ar."AdministrationRouteId" = m."AdministrationRouteId"
        WHERE (@Search IS NULL OR
               m."GenericName"    ILIKE '%' || @Search || '%' OR
               m."CommercialName" ILIKE '%' || @Search || '%')
        ORDER BY m."GenericName"
        LIMIT @Limit OFFSET @Offset
        """;

    /// <summary>
    /// Inserts a new medication and returns the full row (including joined names) via CTE.
    /// </summary>
    internal const string Insert = """
        WITH ins AS (
            INSERT INTO "Medication" (
                "PharmaceuticalFormId", "AdministrationRouteId",
                "GenericName", "CommercialName", "Concentration"
            )
            VALUES (
                @PharmaceuticalFormId, @AdministrationRouteId,
                @GenericName, @CommercialName, @Concentration
            )
            RETURNING *
        )
        SELECT
            ins."MedicationCode",
            ins."PharmaceuticalFormId",
            pf."Name"  AS "PharmaceuticalFormName",
            ins."AdministrationRouteId",
            ar."Name"  AS "AdministrationRouteName",
            ins."GenericName",
            ins."CommercialName",
            ins."Concentration",
            ins."IsActive",
            0          AS "TotalCount"
        FROM ins
        INNER JOIN "PharmaceuticalForm"  pf ON pf."PharmaceuticalFormId"  = ins."PharmaceuticalFormId"
        INNER JOIN "AdministrationRoute" ar ON ar."AdministrationRouteId" = ins."AdministrationRouteId"
        """;

    /// <summary>
    /// Updates an active medication by code and returns the updated full row via CTE.
    /// Returns no rows when the code does not match an active record.
    /// </summary>
    internal const string Update = """
        WITH upd AS (
            UPDATE "Medication"
            SET
                "PharmaceuticalFormId"  = @PharmaceuticalFormId,
                "AdministrationRouteId" = @AdministrationRouteId,
                "GenericName"           = @GenericName,
                "CommercialName"        = @CommercialName,
                "Concentration"         = @Concentration
            WHERE "MedicationCode" = @Code
              AND "IsActive" = TRUE
            RETURNING *
        )
        SELECT
            upd."MedicationCode",
            upd."PharmaceuticalFormId",
            pf."Name"  AS "PharmaceuticalFormName",
            upd."AdministrationRouteId",
            ar."Name"  AS "AdministrationRouteName",
            upd."GenericName",
            upd."CommercialName",
            upd."Concentration",
            upd."IsActive",
            0          AS "TotalCount"
        FROM upd
        INNER JOIN "PharmaceuticalForm"  pf ON pf."PharmaceuticalFormId"  = upd."PharmaceuticalFormId"
        INNER JOIN "AdministrationRoute" ar ON ar."AdministrationRouteId" = upd."AdministrationRouteId"
        """;

    /// <summary>
    /// Soft-deletes an active medication.
    /// <c>DeletedBy</c> is resolved from the caller's <c>UserCode</c> via a subquery.
    /// Affects 0 rows when the code does not match an active record.
    /// </summary>
    internal const string Deactivate = """
        UPDATE "Medication"
        SET
            "IsActive"  = FALSE,
            "DeletedAt" = NOW(),
            "DeletedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode AND "IsActive" = TRUE)
        WHERE "MedicationCode" = @Code
          AND "IsActive" = TRUE
        """;
    
    /// <summary>
    /// Reactivates a previously deactivated medication.
    /// Affects 0 rows when the code does not match an active record.
    /// </summary>
    internal const string Activate = """
        UPDATE "Medication"
        SET
            "IsActive"  = TRUE,
            "DeletedAt" = NULL,
            "DeletedBy" = NULL
        WHERE "MedicationCode" = @Code
          AND "IsActive" = FALSE
        """;
}
