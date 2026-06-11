namespace Infrastructure.Core.Services.Prescription;

/// <summary>
/// SQL used exclusively by <see cref="PrescriptionRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class PrescriptionRepositorySql
{
    private const string DetailAgg = """
                                     COALESCE(
                                         (SELECT json_agg(json_build_object(
                                             'PrescriptionDetailCode',  pd."PrescriptionDetailCode",
                                             'MedicationName',          m."GenericName",
                                             'Dose',                    pd."Dose",
                                             'AdministrationRouteName', ar."Name",
                                             'FrequencyDescription',    f."Description",
                                             'DurationDays',            pd."DurationDays",
                                             'Instructions',            pd."Instructions"
                                         ) ORDER BY pd."PrescriptionDetailId")::text
                                         FROM "PrescriptionDetail" pd
                                         JOIN "Medication" m           ON m."MedicationId"          = pd."MedicationId"
                                         JOIN "AdministrationRoute" ar ON ar."AdministrationRouteId" = pd."AdministrationRouteId"
                                         JOIN "Frequency" f            ON f."FrequencyId"            = pd."FrequencyId"
                                         WHERE pd."PrescriptionId" = p."PrescriptionId"
                                         ),
                                         '[]'
                                     )
                                     """;

    /// <summary>
    /// Returns the full prescription with its detail lines.
    /// Returns no rows when not found or deleted.
    /// </summary>
    internal const string GetByCode = $"""
                                       SELECT
                                           p."PrescriptionCode",
                                           d."DiagnosticCode",
                                           d."Description"              AS "DiagnosticDescription",
                                           pat."PatientCode",
                                           ps."PrescriptionStatusCode"  AS "StatusCode",
                                           ps."Name"                    AS "StatusName",
                                           p."Notes",
                                           p."ValidUntil",
                                           p."CreatedAt",
                                           {DetailAgg} AS "DetailsJson"
                                       FROM "Prescription" p
                                       JOIN "PrescriptionStatus" ps ON ps."PrescriptionStatusId" = p."PrescriptionStatusId"
                                       JOIN "Diagnostic" d          ON d."DiagnosticId"         = p."DiagnosticId"
                                       JOIN "Patient" pat            ON pat."PatientId"          = d."PatientId"
                                       WHERE p."PrescriptionCode" = @Code
                                         AND p."DeletedAt" IS NULL
                                       """;

    /// <summary>
    /// Returns the current status name for a prescription, or NULL if not found / deleted.
    /// Used inside UpdateAsync to distinguish "not found" from "invalid status".
    /// </summary>
    internal const string GetStatusNameByCode = """
                                                SELECT ps."Name"
                                                FROM "Prescription" p
                                                JOIN "PrescriptionStatus" ps ON ps."PrescriptionStatusId" = p."PrescriptionStatusId"
                                                WHERE p."PrescriptionCode" = @Code
                                                  AND p."DeletedAt" IS NULL
                                                """;

    /// <summary>
    /// Inserts a prescription with status Borrador plus all its detail lines, then returns the full row.
    /// Returns no rows when DiagnosticCode does not match an active, non-deleted diagnostic.
    /// Throws a unique constraint violation if a non-deleted prescription already exists for the diagnostic.
    /// </summary>
    internal const string Insert = """
                                   WITH resolved_user AS (
                                       SELECT "UserId" FROM "User" WHERE "UserCode" = @CreatedByUserCode AND "IsActive" = TRUE
                                   ),
                                   resolved_diagnostic AS (
                                       SELECT d."DiagnosticId", d."PatientId"
                                       FROM "Diagnostic" d
                                       WHERE d."DiagnosticCode" = @DiagnosticCode
                                         AND d."DeletedAt" IS NULL
                                   ),
                                   ins_prescription AS (
                                       INSERT INTO "Prescription" ("UserId", "PatientId", "PrescriptionStatusId", "DiagnosticId", "Notes", "ValidUntil", "CreatedBy")
                                       SELECT
                                           ru."UserId",
                                           rd."PatientId",
                                           (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Borrador'),
                                           rd."DiagnosticId",
                                           @Notes,
                                           @ValidUntil,
                                           ru."UserId"
                                       FROM resolved_user ru, resolved_diagnostic rd
                                       RETURNING *
                                   ),
                                   ins_details AS (
                                       INSERT INTO "PrescriptionDetail" ("PrescriptionId", "MedicationId", "AdministrationRouteId", "FrequencyId", "Dose", "DurationDays", "Instructions")
                                       SELECT
                                           ip."PrescriptionId",
                                           m."MedicationId",
                                           ar."AdministrationRouteId",
                                           f."FrequencyId",
                                           (elem->>'Dose')::varchar,
                                           (elem->>'DurationDays')::integer,
                                           elem->>'Instructions'
                                       FROM ins_prescription ip
                                       CROSS JOIN json_array_elements(@DetailsJson::json) AS elem
                                       JOIN "Medication" m           ON m."MedicationCode"          = (elem->>'MedicationCode')::uuid          AND m."IsActive" = TRUE
                                       JOIN "AdministrationRoute" ar ON ar."AdministrationRouteCode" = (elem->>'AdministrationRouteCode')::uuid AND ar."IsActive" = TRUE
                                       JOIN "Frequency" f            ON f."FrequencyCode"            = (elem->>'FrequencyCode')::uuid            AND f."IsActive" = TRUE
                                       RETURNING *
                                   )
                                   SELECT
                                       ip."PrescriptionCode",
                                       d."DiagnosticCode",
                                       d."Description"              AS "DiagnosticDescription",
                                       pat."PatientCode",
                                       ps."PrescriptionStatusCode"  AS "StatusCode",
                                       ps."Name"                    AS "StatusName",
                                       ip."Notes",
                                       ip."ValidUntil",
                                       ip."CreatedAt",
                                       COALESCE(
                                           (SELECT json_agg(json_build_object(
                                               'PrescriptionDetailCode',  id."PrescriptionDetailCode",
                                               'MedicationName',          m."GenericName",
                                               'Dose',                    id."Dose",
                                               'AdministrationRouteName', ar."Name",
                                               'FrequencyDescription',    f."Description",
                                               'DurationDays',            id."DurationDays",
                                               'Instructions',            id."Instructions"
                                           ) ORDER BY id."PrescriptionDetailId")::text
                                           FROM ins_details id
                                           JOIN "Medication" m           ON m."MedicationId"          = id."MedicationId"
                                           JOIN "AdministrationRoute" ar ON ar."AdministrationRouteId" = id."AdministrationRouteId"
                                           JOIN "Frequency" f            ON f."FrequencyId"            = id."FrequencyId"
                                           ),
                                           '[]'
                                       ) AS "DetailsJson"
                                   FROM ins_prescription ip
                                   JOIN "PrescriptionStatus" ps ON ps."PrescriptionStatusId" = ip."PrescriptionStatusId"
                                   JOIN "Diagnostic" d          ON d."DiagnosticId"         = ip."DiagnosticId"
                                   JOIN "Patient" pat            ON pat."PatientId"          = ip."PatientId"
                                   """;

    /// <summary>
    /// Updates notes and validUntil (only when status is Borrador), replaces detail lines, returns full row.
    /// The WHERE clause enforces the Borrador restriction; returns no rows when status differs or not found.
    /// </summary>
    internal const string Update = """
                                   WITH upd_prescription AS (
                                       UPDATE "Prescription"
                                       SET
                                           "Notes"      = @Notes,
                                           "ValidUntil" = @ValidUntil,
                                           "ModifiedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @ModifiedByUserCode AND "IsActive" = TRUE),
                                           "ModifiedAt" = NOW()
                                       WHERE "PrescriptionCode"     = @Code
                                         AND "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Borrador')
                                         AND "DeletedAt"            IS NULL
                                       RETURNING *
                                   ),
                                   del_details AS (
                                       DELETE FROM "PrescriptionDetail"
                                       WHERE "PrescriptionId" IN (SELECT "PrescriptionId" FROM upd_prescription)
                                   ),
                                   ins_details AS (
                                       INSERT INTO "PrescriptionDetail" ("PrescriptionId", "MedicationId", "AdministrationRouteId", "FrequencyId", "Dose", "DurationDays", "Instructions")
                                       SELECT
                                           upd."PrescriptionId",
                                           m."MedicationId",
                                           ar."AdministrationRouteId",
                                           f."FrequencyId",
                                           (elem->>'Dose')::varchar,
                                           (elem->>'DurationDays')::integer,
                                           elem->>'Instructions'
                                       FROM upd_prescription upd
                                       CROSS JOIN json_array_elements(@DetailsJson::json) AS elem
                                       JOIN "Medication" m           ON m."MedicationCode"          = (elem->>'MedicationCode')::uuid          AND m."IsActive" = TRUE
                                       JOIN "AdministrationRoute" ar ON ar."AdministrationRouteCode" = (elem->>'AdministrationRouteCode')::uuid AND ar."IsActive" = TRUE
                                       JOIN "Frequency" f            ON f."FrequencyCode"            = (elem->>'FrequencyCode')::uuid            AND f."IsActive" = TRUE
                                       RETURNING *
                                   )
                                   SELECT
                                       upd."PrescriptionCode",
                                       d."DiagnosticCode",
                                       d."Description"              AS "DiagnosticDescription",
                                       pat."PatientCode",
                                       ps."PrescriptionStatusCode"  AS "StatusCode",
                                       ps."Name"                    AS "StatusName",
                                       upd."Notes",
                                       upd."ValidUntil",
                                       upd."CreatedAt",
                                       COALESCE(
                                           (SELECT json_agg(json_build_object(
                                               'PrescriptionDetailCode',  id."PrescriptionDetailCode",
                                               'MedicationName',          m."GenericName",
                                               'Dose',                    id."Dose",
                                               'AdministrationRouteName', ar."Name",
                                               'FrequencyDescription',    f."Description",
                                               'DurationDays',            id."DurationDays",
                                               'Instructions',            id."Instructions"
                                           ) ORDER BY id."PrescriptionDetailId")::text
                                           FROM ins_details id
                                           JOIN "Medication" m           ON m."MedicationId"          = id."MedicationId"
                                           JOIN "AdministrationRoute" ar ON ar."AdministrationRouteId" = id."AdministrationRouteId"
                                           JOIN "Frequency" f            ON f."FrequencyId"            = id."FrequencyId"
                                           ),
                                           '[]'
                                       ) AS "DetailsJson"
                                   FROM upd_prescription upd
                                   JOIN "PrescriptionStatus" ps ON ps."PrescriptionStatusId" = upd."PrescriptionStatusId"
                                   JOIN "Diagnostic" d          ON d."DiagnosticId"         = upd."DiagnosticId"
                                   JOIN "Patient" pat            ON pat."PatientId"          = upd."PatientId"
                                   """;

    /// <summary>Transitions Borrador → Activo, setting SignedAt and SignedBy.</summary>
    internal const string Sign = """
                                 UPDATE "Prescription"
                                 SET
                                     "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Activo'),
                                     "SignedAt"   = NOW(),
                                     "SignedBy"   = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                     "ModifiedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                     "ModifiedAt" = NOW()
                                 WHERE "PrescriptionCode"     = @Code
                                   AND "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Borrador')
                                   AND "DeletedAt"            IS NULL
                                 """;

    /// <summary>Transitions Activo → Suspendido.</summary>
    internal const string Suspend = """
                                    UPDATE "Prescription"
                                    SET
                                        "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Suspendido'),
                                        "ModifiedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                        "ModifiedAt" = NOW()
                                    WHERE "PrescriptionCode"     = @Code
                                      AND "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Activo')
                                      AND "DeletedAt"            IS NULL
                                    """;

    /// <summary>Transitions Suspendido → Activo.</summary>
    internal const string Reactivate = """
                                       UPDATE "Prescription"
                                       SET
                                           "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Activo'),
                                           "ModifiedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                           "ModifiedAt" = NOW()
                                       WHERE "PrescriptionCode"     = @Code
                                         AND "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Suspendido')
                                         AND "DeletedAt"            IS NULL
                                       """;

    /// <summary>Transitions any non-terminal status (Borrador, Activo, Suspendido) → Cancelado.</summary>
    internal const string Cancel = """
                                   UPDATE "Prescription"
                                   SET
                                       "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Cancelado'),
                                       "ModifiedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                       "ModifiedAt" = NOW()
                                   WHERE "PrescriptionCode"     = @Code
                                     AND "PrescriptionStatusId" IN (
                                         SELECT "PrescriptionStatusId" FROM "PrescriptionStatus"
                                         WHERE "Name" IN ('Borrador', 'Activo', 'Suspendido')
                                     )
                                     AND "DeletedAt" IS NULL
                                   """;

    /// <summary>Transitions Activo → Dispensado, setting DispensedAt and DispensedBy.</summary>
    internal const string Dispense = """
                                     UPDATE "Prescription"
                                     SET
                                         "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Dispensado'),
                                         "DispensedAt" = NOW(),
                                         "DispensedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                         "ModifiedBy"  = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                         "ModifiedAt"  = NOW()
                                     WHERE "PrescriptionCode"     = @Code
                                       AND "PrescriptionStatusId" = (SELECT "PrescriptionStatusId" FROM "PrescriptionStatus" WHERE "Name" = 'Activo')
                                       AND "DeletedAt"            IS NULL
                                     """;
}