namespace Infrastructure.Core.Services.Diagnostic;

/// <summary>
/// SQL used exclusively by <see cref="DiagnosticRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// Diagnostic.PatientId was replaced by Diagnostic.AppointmentId in V008;
/// PatientCode is now obtained via Diagnostic → Appointment → Patient.
/// </summary>
internal static class DiagnosticRepositorySql
{
    private const string PrescriptionSummarySubquery = """
                                                       (
                                                           SELECT json_build_object(
                                                               'PrescriptionCode', p."PrescriptionCode",
                                                               'StatusCode',       ps."PrescriptionStatusCode",
                                                               'StatusName',       ps."Name",
                                                               'ValidUntil',       TO_CHAR(p."ValidUntil", 'YYYY-MM-DD'),
                                                               'DetailCount',      (
                                                                   SELECT COUNT(*)::int
                                                                   FROM "PrescriptionDetail" pd
                                                                   WHERE pd."PrescriptionId" = p."PrescriptionId"
                                                                     AND pd."DeletedAt" IS NULL
                                                               )
                                                           )::text
                                                           FROM "Prescription" p
                                                           JOIN "PrescriptionStatus" ps ON ps."PrescriptionStatusId" = p."PrescriptionStatusId"
                                                           WHERE p."DiagnosticId" = d."DiagnosticId"
                                                             AND p."DeletedAt" IS NULL
                                                       )
                                                       """;

    /// <summary>
    /// Returns one page of diagnostics for a patient with their prescription summary (if any).
    /// PatientCode is resolved via Diagnostic → Appointment → Patient.
    /// </summary>
    internal const string GetPage = $"""
                                     SELECT
                                         d."DiagnosticCode",
                                         appt."AppointmentCode",
                                         pat."PatientCode",
                                         ds."DiagnosticStatusCode" AS "StatusCode",
                                         ds."Name"                 AS "StatusName",
                                         d."Description",
                                         d."DiagnosedAt",
                                         d."Notes",
                                         d."CreatedAt",
                                         {PrescriptionSummarySubquery} AS "PrescriptionSummaryJson",
                                         COUNT(*) OVER () AS "TotalCount"
                                     FROM "Diagnostic" d
                                     JOIN "DiagnosticStatus" ds ON ds."DiagnosticStatusId"  = d."DiagnosticStatusId"
                                     JOIN "Appointment" appt    ON appt."AppointmentId"     = d."AppointmentId"
                                     JOIN "Patient" pat         ON pat."PatientId"          = appt."PatientId"
                                     WHERE pat."PatientCode" = @PatientCode
                                       AND d."DeletedAt" IS NULL
                                     ORDER BY d."DiagnosedAt" DESC, d."DiagnosticId" DESC
                                     LIMIT @Limit OFFSET @Offset
                                     """;

    /// <summary>
    /// Inserts a diagnostic with status Activo linked to the given appointment.
    /// Returns no rows when:
    ///   - the appointment does not exist, or
    ///   - the appointment status is not Confirmado or Completado.
    /// Throws a unique constraint exception (uq_diagnostic_appointment) when a non-deleted
    /// diagnostic already exists for the appointment.
    /// </summary>
    internal const string Insert = """
                                   WITH ins AS (
                                       INSERT INTO "Diagnostic"
                                           ("AppointmentId", "DiagnosticStatusId", "Description", "DiagnosedAt", "Notes", "CreatedBy")
                                       SELECT
                                           appt."AppointmentId",
                                           (SELECT "DiagnosticStatusId" FROM "DiagnosticStatus" WHERE "Name" = 'Activo'),
                                           @Description,
                                           @DiagnosedAt,
                                           @Notes,
                                           (SELECT "UserId" FROM "User" WHERE "UserCode" = @CreatedByUserCode AND "IsActive" = TRUE)
                                       FROM "Appointment" appt
                                       WHERE appt."AppointmentCode" = @AppointmentCode
                                         AND appt."AppointmentStatusId" IN (
                                             SELECT "AppointmentStatusId" FROM "AppointmentStatus"
                                             WHERE "Name" IN ('Confirmado', 'Completado')
                                         )
                                       RETURNING *
                                   )
                                   SELECT
                                       ins."DiagnosticCode",
                                       appt."AppointmentCode",
                                       pat."PatientCode",
                                       ds."DiagnosticStatusCode" AS "StatusCode",
                                       ds."Name"                 AS "StatusName",
                                       ins."Description",
                                       ins."DiagnosedAt",
                                       ins."Notes",
                                       ins."CreatedAt",
                                       NULL::text AS "PrescriptionSummaryJson",
                                       0          AS "TotalCount"
                                   FROM ins
                                   JOIN "DiagnosticStatus" ds ON ds."DiagnosticStatusId" = ins."DiagnosticStatusId"
                                   JOIN "Appointment" appt    ON appt."AppointmentId"    = ins."AppointmentId"
                                   JOIN "Patient" pat         ON pat."PatientId"         = appt."PatientId"
                                   """;

    /// <summary>
    /// Updates description, date and notes. Returns no rows when not found or deleted.
    /// PatientCode is resolved via Diagnostic → Appointment → Patient.
    /// </summary>
    internal const string Update = """
                                   WITH upd AS (
                                       UPDATE "Diagnostic"
                                       SET
                                           "Description" = @Description,
                                           "DiagnosedAt" = @DiagnosedAt,
                                           "Notes"       = @Notes,
                                           "ModifiedBy"  = (SELECT "UserId" FROM "User" WHERE "UserCode" = @ModifiedByUserCode AND "IsActive" = TRUE),
                                           "ModifiedAt"  = NOW()
                                       WHERE "DiagnosticCode" = @Code
                                         AND "DeletedAt" IS NULL
                                       RETURNING *
                                   ), d AS (SELECT * FROM upd)
                                   SELECT
                                       d."DiagnosticCode",
                                       appt."AppointmentCode",
                                       pat."PatientCode",
                                       ds."DiagnosticStatusCode" AS "StatusCode",
                                       ds."Name"                 AS "StatusName",
                                       d."Description",
                                       d."DiagnosedAt",
                                       d."Notes",
                                       d."CreatedAt",
                                       (
                                           SELECT json_build_object(
                                               'PrescriptionCode', p."PrescriptionCode",
                                               'StatusCode',       ps."PrescriptionStatusCode",
                                               'StatusName',       ps."Name",
                                               'ValidUntil',       TO_CHAR(p."ValidUntil", 'YYYY-MM-DD'),
                                               'DetailCount',      (
                                                   SELECT COUNT(*)::int
                                                   FROM "PrescriptionDetail" pd
                                                   WHERE pd."PrescriptionId" = p."PrescriptionId"
                                                     AND pd."DeletedAt" IS NULL
                                               )
                                           )::text
                                           FROM "Prescription" p
                                           JOIN "PrescriptionStatus" ps ON ps."PrescriptionStatusId" = p."PrescriptionStatusId"
                                           WHERE p."DiagnosticId" = d."DiagnosticId"
                                             AND p."DeletedAt" IS NULL
                                       ) AS "PrescriptionSummaryJson",
                                       0 AS "TotalCount"
                                   FROM d
                                   JOIN "DiagnosticStatus" ds ON ds."DiagnosticStatusId" = d."DiagnosticStatusId"
                                   JOIN "Appointment" appt    ON appt."AppointmentId"    = d."AppointmentId"
                                   JOIN "Patient" pat         ON pat."PatientId"         = appt."PatientId"
                                   """;

    /// <summary>
    /// Transitions Activo → Inactivo. Only updates if currently Activo and not deleted.
    /// </summary>
    internal const string Deactivate = """
                                       UPDATE "Diagnostic"
                                       SET
                                           "DiagnosticStatusId" = (SELECT "DiagnosticStatusId" FROM "DiagnosticStatus" WHERE "Name" = 'Inactivo'),
                                           "ModifiedBy"         = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                           "ModifiedAt"         = NOW()
                                       WHERE "DiagnosticCode"     = @Code
                                         AND "DiagnosticStatusId" = (SELECT "DiagnosticStatusId" FROM "DiagnosticStatus" WHERE "Name" = 'Activo')
                                         AND "DeletedAt"          IS NULL
                                       """;

    /// <summary>
    /// Transitions Inactivo → Activo. Only updates if currently Inactivo and not deleted.
    /// </summary>
    internal const string Activate = """
                                     UPDATE "Diagnostic"
                                     SET
                                         "DiagnosticStatusId" = (SELECT "DiagnosticStatusId" FROM "DiagnosticStatus" WHERE "Name" = 'Activo'),
                                         "ModifiedBy"         = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE),
                                         "ModifiedAt"         = NOW()
                                     WHERE "DiagnosticCode"     = @Code
                                       AND "DiagnosticStatusId" = (SELECT "DiagnosticStatusId" FROM "DiagnosticStatus" WHERE "Name" = 'Inactivo')
                                       AND "DeletedAt"          IS NULL
                                     """;
}