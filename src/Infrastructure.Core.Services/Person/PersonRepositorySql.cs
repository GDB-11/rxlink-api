namespace Infrastructure.Core.Services.Person;

internal static class PersonRepositorySql
{
    internal const string GetPage = """
                                    SELECT
                                        p."PersonCode",
                                        p."Names",
                                        p."Surnames",
                                        p."BirthDate",
                                        p."SexCode",
                                        p."Phone",
                                        p."AlternativePhone",
                                        p."Email",
                                        p."Address",
                                        p."EmergencyContactName",
                                        p."EmergencyContactPhone",
                                        doc."DocumentTypeCode",
                                        doc."DocumentNumber",
                                        COUNT(*) OVER () AS "TotalCount"
                                    FROM "Person" p
                                    LEFT JOIN LATERAL (
                                        SELECT pd."DocumentTypeCode",
                                               pd."Number" AS "DocumentNumber"
                                        FROM   "PersonDocument" pd
                                        WHERE  pd."PersonId" = p."PersonId"
                                        ORDER  BY pd."PersonDocumentId"
                                        LIMIT  1
                                    ) doc ON TRUE
                                    WHERE (@Search IS NULL
                                           OR p."Names"    ILIKE '%' || @Search || '%'
                                           OR p."Surnames" ILIKE '%' || @Search || '%')
                                    ORDER BY p."Surnames", p."Names"
                                    LIMIT @Limit OFFSET @Offset
                                    """;

    /// <summary>
    /// Same projection as <see cref="GetPage"/> but adds optional NOT EXISTS filters to exclude
    /// persons already linked to a User or Patient record. Used by the picker endpoint only.
    /// </summary>
    internal const string GetAvailable = """
                                         SELECT
                                             p."PersonCode",
                                             p."Names",
                                             p."Surnames",
                                             p."BirthDate",
                                             p."SexCode",
                                             p."Phone",
                                             p."AlternativePhone",
                                             p."Email",
                                             p."Address",
                                             p."EmergencyContactName",
                                             p."EmergencyContactPhone",
                                             doc."DocumentTypeCode",
                                             doc."DocumentNumber",
                                             COUNT(*) OVER () AS "TotalCount"
                                         FROM "Person" p
                                         LEFT JOIN LATERAL (
                                             SELECT pd."DocumentTypeCode",
                                                    pd."Number" AS "DocumentNumber"
                                             FROM   "PersonDocument" pd
                                             WHERE  pd."PersonId" = p."PersonId"
                                             ORDER  BY pd."PersonDocumentId"
                                             LIMIT  1
                                         ) doc ON TRUE
                                         WHERE (@Search IS NULL
                                                OR p."Names"    ILIKE '%' || @Search || '%'
                                                OR p."Surnames" ILIKE '%' || @Search || '%')
                                           AND (NOT @ExcludeLinkedUsers    OR NOT EXISTS (
                                                   SELECT 1 FROM "User" u WHERE u."PersonId" = p."PersonId"
                                               ))
                                           AND (NOT @ExcludeLinkedPatients OR NOT EXISTS (
                                                   SELECT 1 FROM "Patient" pt WHERE pt."PersonId" = p."PersonId"
                                               ))
                                         ORDER BY p."Surnames", p."Names"
                                         LIMIT @Limit OFFSET @Offset
                                         """;

    internal const string GetByCode = """
                                      SELECT
                                          p."PersonCode",
                                          p."Names",
                                          p."Surnames",
                                          p."BirthDate",
                                          p."SexCode",
                                          p."Phone",
                                          p."AlternativePhone",
                                          p."Email",
                                          p."Address",
                                          p."EmergencyContactName",
                                          p."EmergencyContactPhone",
                                          doc."DocumentTypeCode",
                                          doc."DocumentNumber",
                                          1 AS "TotalCount"
                                      FROM "Person" p
                                      LEFT JOIN LATERAL (
                                          SELECT pd."DocumentTypeCode",
                                                 pd."Number" AS "DocumentNumber"
                                          FROM   "PersonDocument" pd
                                          WHERE  pd."PersonId" = p."PersonId"
                                          ORDER  BY pd."PersonDocumentId"
                                          LIMIT  1
                                      ) doc ON TRUE
                                      WHERE p."PersonCode" = @Code
                                      """;

    internal const string Insert = """
                                   WITH ins AS (
                                       INSERT INTO "Person" (
                                           "Names", "Surnames", "BirthDate", "SexId", "SexCode",
                                           "Phone", "AlternativePhone", "Email",
                                           "Address", "EmergencyContactName", "EmergencyContactPhone"
                                       )
                                       SELECT
                                           @Names, @Surnames, @BirthDate, s."SexId", s."SexCode",
                                           @Phone, @AlternativePhone, @Email,
                                           @Address, @EmergencyContactName, @EmergencyContactPhone
                                       FROM "Sex" s
                                       WHERE s."SexCode" = @SexCode
                                       RETURNING *
                                   ),
                                   doc_ins AS (
                                       INSERT INTO "PersonDocument" ("PersonId", "DocumentTypeId", "DocumentTypeCode", "Number")
                                       SELECT i."PersonId", dt."DocumentTypeId", dt."DocumentTypeCode", @DocumentNumber
                                       FROM ins i
                                       JOIN "DocumentType" dt ON dt."DocumentTypeCode" = @DocumentTypeCode
                                       RETURNING "PersonId", "DocumentTypeCode", "Number"
                                   ),
                                   mrn AS (
                                       SELECT 'PAC-' || TO_CHAR(NOW(), 'YYYYMM') || '-' ||
                                              LPAD(
                                                  (COALESCE(MAX(SPLIT_PART("MedicalRecordNumber", '-', 3)::INTEGER), 0) + 1)::TEXT,
                                                  5, '0'
                                              ) AS value
                                       FROM "Patient"
                                       WHERE "MedicalRecordNumber" LIKE 'PAC-' || TO_CHAR(NOW(), 'YYYYMM') || '-%'
                                   ),
                                   ins_patient AS (
                                       INSERT INTO "Patient" ("PersonId", "MedicalRecordNumber", "PasswordHash")
                                       SELECT ins."PersonId", mrn.value, @PasswordHash
                                       FROM ins, mrn
                                       RETURNING *
                                   ),
                                   ins_allergies AS (
                                       INSERT INTO "PatientAllergy" ("PatientId", "AllergyId", "AllergyCode", "SeverityId", "SeverityCode", "Notes")
                                       SELECT ins_patient."PatientId", a."AllergyId", a."AllergyCode", sv."SeverityId", sv."SeverityCode", elem->>'Notes'
                                       FROM ins_patient
                                       CROSS JOIN json_array_elements(COALESCE(@AllergiesJson::json, '[]'::json)) AS elem
                                       JOIN "Allergy" a ON a."AllergyCode" = (elem->>'AllergyCode')::uuid
                                                        AND a."IsActive" = TRUE
                                       LEFT JOIN "AllergySeverity" sv ON sv."SeverityCode" = (elem->>'SeverityCode')::uuid
                                                                     AND sv."IsActive" = TRUE
                                   )
                                   SELECT
                                       ins."PersonCode",
                                       ins."Names",
                                       ins."Surnames",
                                       ins."BirthDate",
                                       ins."SexCode",
                                       ins."Phone",
                                       ins."AlternativePhone",
                                       ins."Email",
                                       ins."Address",
                                       ins."EmergencyContactName",
                                       ins."EmergencyContactPhone",
                                       doc_ins."DocumentTypeCode",
                                       doc_ins."Number" AS "DocumentNumber",
                                       0 AS "TotalCount"
                                   FROM ins
                                   JOIN doc_ins ON doc_ins."PersonId" = ins."PersonId"
                                   JOIN ins_patient ON ins_patient."PersonId" = ins."PersonId"
                                   """;

    internal const string Update = """
                                   WITH upd AS (
                                       UPDATE "Person"
                                       SET
                                           "Names"                 = @Names,
                                           "Surnames"              = @Surnames,
                                           "BirthDate"             = @BirthDate,
                                           "SexId"                 = (SELECT "SexId" FROM "Sex" WHERE "SexCode" = @SexCode),
                                           "SexCode"               = (SELECT "SexCode" FROM "Sex" WHERE "SexCode" = @SexCode),
                                           "Phone"                 = @Phone,
                                           "AlternativePhone"      = @AlternativePhone,
                                           "Email"                 = @Email,
                                           "Address"               = @Address,
                                           "EmergencyContactName"  = @EmergencyContactName,
                                           "EmergencyContactPhone" = @EmergencyContactPhone
                                       WHERE "PersonCode" = @Code
                                       RETURNING *
                                   ),
                                   doc_del AS (
                                       DELETE FROM "PersonDocument"
                                       WHERE "PersonId" = (SELECT "PersonId" FROM upd)
                                   ),
                                   doc_ins AS (
                                       INSERT INTO "PersonDocument" ("PersonId", "DocumentTypeId", "DocumentTypeCode", "Number")
                                       SELECT u."PersonId", dt."DocumentTypeId", dt."DocumentTypeCode", @DocumentNumber
                                       FROM upd u
                                       JOIN "DocumentType" dt ON dt."DocumentTypeCode" = @DocumentTypeCode
                                       RETURNING "PersonId", "DocumentTypeCode", "Number"
                                   )
                                   SELECT
                                       upd."PersonCode",
                                       upd."Names",
                                       upd."Surnames",
                                       upd."BirthDate",
                                       upd."SexCode",
                                       upd."Phone",
                                       upd."AlternativePhone",
                                       upd."Email",
                                       upd."Address",
                                       upd."EmergencyContactName",
                                       upd."EmergencyContactPhone",
                                       doc_ins."DocumentTypeCode",
                                       doc_ins."Number" AS "DocumentNumber",
                                       0 AS "TotalCount"
                                   FROM upd
                                   JOIN doc_ins ON doc_ins."PersonId" = upd."PersonId"
                                   """;
}