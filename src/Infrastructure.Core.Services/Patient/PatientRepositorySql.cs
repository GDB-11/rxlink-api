namespace Infrastructure.Core.Services.Patient;

/// <summary>
/// SQL used exclusively by <see cref="PatientRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// Person data is managed exclusively through /api/person; patient operations only link to an existing Person.
/// MedicalRecordNumber is auto-generated (PAC-YYYYMM-NNNNN, resets each month).
/// </summary>
internal static class PatientRepositorySql
{
    private const string AllergyAgg = """
                                      COALESCE(
                                          (SELECT json_agg(json_build_object(
                                              'PatientAllergyCode', pa."PatientAllergyCode",
                                              'AllergyCode',        a."AllergyCode",
                                              'AllergyName',        a."Name",
                                              'SeverityCode',       sv."SeverityCode",
                                              'SeverityName',       sv."Name",
                                              'Notes',              pa."Notes"
                                          ) ORDER BY a."Name")
                                          FROM "PatientAllergy" pa
                                          JOIN "Allergy" a ON a."AllergyId" = pa."AllergyId"
                                          LEFT JOIN "AllergySeverity" sv ON sv."SeverityId" = pa."SeverityId"
                                          WHERE pa."PatientId" = pat."PatientId"
                                            AND pa."DeletedAt" IS NULL
                                          ),
                                          '[]'::json
                                      )::text
                                      """;

    /// <summary>
    /// Returns one page of patients with their allergy list (JSON-aggregated).
    /// Optional <c>@Search</c> filters by names or surnames (case-insensitive).
    /// </summary>
    internal const string GetPage = $"""
                                     SELECT
                                         pat."PatientCode",
                                         pe."PersonCode",
                                         pat."MedicalRecordNumber",
                                         pat."IsActive",
                                         pe."Names",
                                         pe."Surnames",
                                         pe."BirthDate",
                                         pe."Phone",
                                         pe."AlternativePhone",
                                         pe."Email",
                                         pe."Address",
                                         pe."EmergencyContactName",
                                         pe."EmergencyContactPhone",
                                         {AllergyAgg} AS "AllergiesJson",
                                         COUNT(*) OVER () AS "TotalCount"
                                     FROM "Patient" pat
                                     INNER JOIN "Person" pe ON pe."PersonId" = pat."PersonId"
                                     WHERE (@Search IS NULL
                                            OR pe."Names"    ILIKE '%' || @Search || '%'
                                            OR pe."Surnames" ILIKE '%' || @Search || '%')
                                     ORDER BY pe."Surnames", pe."Names"
                                     LIMIT @Limit OFFSET @Offset
                                     """;

    /// <summary>
    /// Auto-generates MedicalRecordNumber (PAC-YYYYMM-NNNNN), links to an existing Person by PersonCode,
    /// inserts PatientAllergy rows from a JSON array, and returns the full patient row.
    /// Returns no rows when PersonCode does not match any registered person.
    /// </summary>
    internal const string Insert = """
                                   WITH mrn AS (
                                       SELECT 'PAC-' || TO_CHAR(NOW(), 'YYYYMM') || '-' ||
                                              LPAD(
                                                  (COALESCE(
                                                      MAX(SPLIT_PART("MedicalRecordNumber", '-', 3)::INTEGER),
                                                      0
                                                  ) + 1)::TEXT,
                                                  5, '0'
                                              ) AS value
                                       FROM "Patient"
                                       WHERE "MedicalRecordNumber" LIKE 'PAC-' || TO_CHAR(NOW(), 'YYYYMM') || '-%'
                                   ),
                                   ins_patient AS (
                                       INSERT INTO "Patient" ("PersonId", "MedicalRecordNumber")
                                       SELECT p."PersonId", mrn.value
                                       FROM "Person" p, mrn
                                       WHERE p."PersonCode" = @PersonCode
                                       RETURNING *
                                   ),
                                   ins_allergies AS (
                                       INSERT INTO "PatientAllergy" ("PatientId", "AllergyId", "SeverityId", "Notes")
                                       SELECT ip."PatientId", a."AllergyId", sv."SeverityId", elem->>'Notes'
                                       FROM ins_patient ip
                                       CROSS JOIN json_array_elements(COALESCE(@AllergiesJson::json, '[]'::json)) AS elem
                                       JOIN "Allergy" a ON a."AllergyCode" = (elem->>'AllergyCode')::uuid
                                                        AND a."IsActive" = TRUE
                                       LEFT JOIN "AllergySeverity" sv ON sv."SeverityCode" = (elem->>'SeverityCode')::uuid
                                                                     AND sv."IsActive" = TRUE
                                       RETURNING *
                                   )
                                   SELECT
                                       ip."PatientCode",
                                       pe."PersonCode",
                                       ip."MedicalRecordNumber",
                                       ip."IsActive",
                                       pe."Names",
                                       pe."Surnames",
                                       pe."BirthDate",
                                       pe."Phone",
                                       pe."AlternativePhone",
                                       pe."Email",
                                       pe."Address",
                                       pe."EmergencyContactName",
                                       pe."EmergencyContactPhone",
                                       COALESCE(
                                           (SELECT json_agg(json_build_object(
                                               'PatientAllergyCode', ia."PatientAllergyCode",
                                               'AllergyCode',        a."AllergyCode",
                                               'AllergyName',        a."Name",
                                               'SeverityCode',       sv."SeverityCode",
                                               'SeverityName',       sv."Name",
                                               'Notes',              ia."Notes"
                                           ) ORDER BY a."Name")
                                           FROM ins_allergies ia
                                           JOIN "Allergy" a ON a."AllergyId" = ia."AllergyId"
                                           LEFT JOIN "AllergySeverity" sv ON sv."SeverityId" = ia."SeverityId"
                                           WHERE ia."PatientId" = ip."PatientId"
                                           ),
                                           '[]'::json
                                       )::text AS "AllergiesJson",
                                       0 AS "TotalCount"
                                   FROM ins_patient ip
                                   INNER JOIN "Person" pe ON pe."PersonId" = ip."PersonId"
                                   """;

    /// <summary>
    /// Updates MedicalRecordNumber for an active patient and returns the full row with current allergies.
    /// Returns no rows when the code does not match an active record.
    /// </summary>
    internal const string Update = """
                                   WITH upd_patient AS (
                                       UPDATE "Patient"
                                       SET "MedicalRecordNumber" = @MedicalRecordNumber
                                       WHERE "PatientCode" = @Code
                                         AND "IsActive" = TRUE
                                       RETURNING *
                                   )
                                   SELECT
                                       upd_patient."PatientCode",
                                       pe."PersonCode",
                                       upd_patient."MedicalRecordNumber",
                                       upd_patient."IsActive",
                                       pe."Names",
                                       pe."Surnames",
                                       pe."BirthDate",
                                       pe."Phone",
                                       pe."AlternativePhone",
                                       pe."Email",
                                       pe."Address",
                                       pe."EmergencyContactName",
                                       pe."EmergencyContactPhone",
                                       COALESCE(
                                           (SELECT json_agg(json_build_object(
                                               'PatientAllergyCode', pa."PatientAllergyCode",
                                               'AllergyCode',        a."AllergyCode",
                                               'AllergyName',        a."Name",
                                               'SeverityCode',       sv."SeverityCode",
                                               'SeverityName',       sv."Name",
                                               'Notes',              pa."Notes"
                                           ) ORDER BY a."Name")
                                           FROM "PatientAllergy" pa
                                           JOIN "Allergy" a ON a."AllergyId" = pa."AllergyId"
                                           LEFT JOIN "AllergySeverity" sv ON sv."SeverityId" = pa."SeverityId"
                                           WHERE pa."PatientId" = upd_patient."PatientId"
                                             AND pa."DeletedAt" IS NULL
                                           ),
                                           '[]'::json
                                       )::text AS "AllergiesJson",
                                       0 AS "TotalCount"
                                   FROM upd_patient
                                   INNER JOIN "Person" pe ON pe."PersonId" = upd_patient."PersonId"
                                   """;

    /// <summary>
    /// Soft-deletes an active patient.
    /// </summary>
    internal const string Deactivate = """
                                       UPDATE "Patient"
                                       SET
                                           "IsActive"  = FALSE,
                                           "DeletedAt" = NOW(),
                                           "DeletedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE)
                                       WHERE "PatientCode" = @Code
                                         AND "IsActive" = TRUE
                                       """;

    /// <summary>
    /// Reactivates a previously deactivated patient.
    /// </summary>
    internal const string Activate = """
                                     UPDATE "Patient"
                                     SET
                                         "IsActive"  = TRUE,
                                         "DeletedAt" = NULL,
                                         "DeletedBy" = NULL
                                     WHERE "PatientCode" = @Code
                                       AND "IsActive" = FALSE
                                     """;

    /// <summary>
    /// Inserts a new PatientAllergy or restores a previously soft-deleted one (upsert).
    /// Returns no rows when PatientCode or AllergyCode are not found.
    /// </summary>
    internal const string AddAllergy = """
                                       WITH ins AS (
                                           INSERT INTO "PatientAllergy" ("PatientId", "AllergyId", "SeverityId", "Notes")
                                           SELECT
                                               pat."PatientId",
                                               a."AllergyId",
                                               (SELECT "SeverityId" FROM "AllergySeverity" WHERE "SeverityCode" = @SeverityCode AND "IsActive" = TRUE),
                                               @Notes
                                           FROM "Patient" pat
                                           JOIN "Allergy" a ON a."AllergyCode" = @AllergyCode AND a."IsActive" = TRUE
                                           WHERE pat."PatientCode" = @PatientCode
                                             AND pat."IsActive" = TRUE
                                           ON CONFLICT ON CONSTRAINT uq_patient_allergy
                                           DO UPDATE SET
                                               "SeverityId" = EXCLUDED."SeverityId",
                                               "Notes"      = EXCLUDED."Notes",
                                               "DeletedAt"  = NULL,
                                               "DeletedBy"  = NULL
                                           RETURNING *
                                       )
                                       SELECT
                                           ins."PatientAllergyCode",
                                           a."AllergyCode",
                                           a."Name"  AS "AllergyName",
                                           sv."SeverityCode",
                                           sv."Name" AS "SeverityName",
                                           ins."Notes"
                                       FROM ins
                                       JOIN "Allergy" a ON a."AllergyId" = ins."AllergyId"
                                       LEFT JOIN "AllergySeverity" sv ON sv."SeverityId" = ins."SeverityId"
                                       """;

    /// <summary>
    /// Updates the severity and notes of a patient allergy identified by its code.
    /// Returns no rows when not found or already deleted.
    /// </summary>
    internal const string UpdateAllergy = """
                                          WITH upd AS (
                                              UPDATE "PatientAllergy"
                                              SET
                                                  "SeverityId" = (SELECT "SeverityId" FROM "AllergySeverity" WHERE "SeverityCode" = @SeverityCode AND "IsActive" = TRUE),
                                                  "Notes"      = @Notes
                                              WHERE "PatientAllergyCode" = @PatientAllergyCode
                                                AND "PatientId" = (SELECT "PatientId" FROM "Patient" WHERE "PatientCode" = @PatientCode AND "IsActive" = TRUE)
                                                AND "DeletedAt" IS NULL
                                              RETURNING *
                                          )
                                          SELECT
                                              upd."PatientAllergyCode",
                                              a."AllergyCode",
                                              a."Name"  AS "AllergyName",
                                              sv."SeverityCode",
                                              sv."Name" AS "SeverityName",
                                              upd."Notes"
                                          FROM upd
                                          JOIN "Allergy" a ON a."AllergyId" = upd."AllergyId"
                                          LEFT JOIN "AllergySeverity" sv ON sv."SeverityId" = upd."SeverityId"
                                          """;

    /// <summary>
    /// Returns the full patient row for a single patient identified by PatientCode.
    /// Returns no rows when not found or inactive.
    /// </summary>
    internal const string GetByCode = $"""
                                       SELECT
                                           pat."PatientCode",
                                           pe."PersonCode",
                                           pat."MedicalRecordNumber",
                                           pat."IsActive",
                                           pe."Names",
                                           pe."Surnames",
                                           pe."BirthDate",
                                           pe."Phone",
                                           pe."AlternativePhone",
                                           pe."Email",
                                           pe."Address",
                                           pe."EmergencyContactName",
                                           pe."EmergencyContactPhone",
                                           {AllergyAgg} AS "AllergiesJson",
                                           0 AS "TotalCount"
                                       FROM "Patient" pat
                                       INNER JOIN "Person" pe ON pe."PersonId" = pat."PersonId"
                                       WHERE pat."PatientCode" = @PatientCode
                                         AND pat."IsActive" = TRUE
                                       """;

    /// <summary>
    /// Updates the contact fields on the Person linked to the active Patient identified by @PatientCode.
    /// Returns the number of updated rows (0 = patient not found or inactive).
    /// </summary>
    internal const string UpdatePersonContact = """
                                                UPDATE "Person"
                                                SET
                                                    "Phone"                 = @Phone,
                                                    "AlternativePhone"      = @AlternativePhone,
                                                    "Address"               = @Address,
                                                    "EmergencyContactName"  = @EmergencyContactName,
                                                    "EmergencyContactPhone" = @EmergencyContactPhone
                                                FROM "Patient" pat
                                                WHERE "Person"."PersonId" = pat."PersonId"
                                                  AND pat."PatientCode"   = @PatientCode
                                                  AND pat."IsActive"      = TRUE
                                                """;

    /// <summary>
    /// Soft-deletes a patient allergy record.
    /// </summary>
    internal const string DeleteAllergy = """
                                          UPDATE "PatientAllergy"
                                          SET
                                              "DeletedAt" = NOW(),
                                              "DeletedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE)
                                          WHERE "PatientAllergyCode" = @PatientAllergyCode
                                            AND "PatientId" = (SELECT "PatientId" FROM "Patient" WHERE "PatientCode" = @PatientCode AND "IsActive" = TRUE)
                                            AND "DeletedAt" IS NULL
                                          """;
}