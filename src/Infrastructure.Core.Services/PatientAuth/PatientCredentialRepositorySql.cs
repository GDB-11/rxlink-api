namespace Infrastructure.Core.Services.PatientAuth;

internal static class PatientCredentialRepositorySql
{
    private const string PatientCredentialProjection = """
                                                       pat."PatientCode",
                                                       pe."PersonCode",
                                                       pe."Email",
                                                       pe."Names",
                                                       pe."Surnames",
                                                       pat."MedicalRecordNumber",
                                                       pat."PasswordHash",
                                                       pat."IsActive"
                                                       """;

    private const string MrnCte = """
                                  mrn AS (
                                      SELECT 'PAC-' || TO_CHAR(NOW(), 'YYYYMM') || '-' ||
                                             LPAD(
                                                 (COALESCE(MAX(SPLIT_PART("MedicalRecordNumber", '-', 3)::INTEGER), 0) + 1)::TEXT,
                                                 5, '0'
                                             ) AS value
                                      FROM "Patient"
                                      WHERE "MedicalRecordNumber" LIKE 'PAC-' || TO_CHAR(NOW(), 'YYYYMM') || '-%'
                                  )
                                  """;

    internal const string GetByDocument = """
                                          SELECT
                                              p."PersonCode",
                                              p."Names",
                                              p."Surnames",
                                              p."Email",
                                              pat."PatientCode",
                                              (pat."PasswordHash" IS NOT NULL) AS "HasCredentials"
                                          FROM "PersonDocument" pd
                                          JOIN "DocumentType" dt  ON dt."DocumentTypeId" = pd."DocumentTypeId"
                                          JOIN "Person" p          ON p."PersonId"        = pd."PersonId"
                                          LEFT JOIN "Patient" pat  ON pat."PersonId"       = p."PersonId"
                                                                   AND pat."IsActive"      = TRUE
                                          WHERE dt."DocumentTypeCode" = @DocumentTypeCode
                                            AND pd."Number"           = @DocumentNumber
                                          """;

    internal const string GetByPersonCode = """
                                            SELECT
                                                p."PersonCode",
                                                p."Names",
                                                p."Surnames",
                                                p."Email",
                                                pat."PatientCode",
                                                (pat."PasswordHash" IS NOT NULL) AS "HasCredentials"
                                            FROM "Person" p
                                            LEFT JOIN "Patient" pat ON pat."PersonId" = p."PersonId"
                                                                    AND pat."IsActive" = TRUE
                                            WHERE p."PersonCode" = @PersonCode
                                            """;

    internal const string GetByEmail = $"""
                                        SELECT {PatientCredentialProjection}
                                        FROM "Patient" pat
                                        JOIN "Person" pe ON pe."PersonId" = pat."PersonId"
                                        WHERE pe."Email"   = @Email
                                          AND pat."IsActive" = TRUE
                                        """;

    internal const string GetByRefreshToken = $"""
                                               SELECT {PatientCredentialProjection}
                                               FROM "Patient" pat
                                               JOIN "Person" pe               ON pe."PersonId"  = pat."PersonId"
                                               JOIN "PatientRefreshToken" prt ON prt."PatientId" = pat."PatientId"
                                               WHERE prt."TokenHash" = @TokenHash
                                                 AND prt."ExpiresAt" > @CurrentDate
                                                 AND pat."IsActive"  = TRUE
                                               """;

    internal const string CreatePersonAndPatient = $"""
                                                    WITH ins_person AS (
                                                        INSERT INTO "Person" (
                                                            "Names", "Surnames", "BirthDate", "SexId",
                                                            "Phone", "AlternativePhone", "Email",
                                                            "Address", "EmergencyContactName", "EmergencyContactPhone"
                                                        )
                                                        SELECT
                                                            @Names, @Surnames, @BirthDate,
                                                            s."SexId",
                                                            @Phone, @AlternativePhone, @Email,
                                                            @Address, @EmergencyContactName, @EmergencyContactPhone
                                                        FROM "Sex" s
                                                        WHERE s."SexCode" = @SexCode
                                                        RETURNING *
                                                    ),
                                                    ins_doc AS (
                                                        INSERT INTO "PersonDocument" ("PersonId", "DocumentTypeId", "Number")
                                                        SELECT ip."PersonId", dt."DocumentTypeId", @DocumentNumber
                                                        FROM ins_person ip
                                                        JOIN "DocumentType" dt ON dt."DocumentTypeCode" = @DocumentTypeCode
                                                        RETURNING "PersonId"
                                                    ),
                                                    {MrnCte},
                                                    ins_patient AS (
                                                        INSERT INTO "Patient" ("PersonId", "MedicalRecordNumber", "PasswordHash")
                                                        SELECT ip."PersonId", mrn.value, @PasswordHash
                                                        FROM ins_person ip, mrn
                                                        RETURNING *
                                                    ),
                                                    ins_token AS (
                                                        INSERT INTO "PatientRefreshToken" ("PatientId")
                                                        SELECT "PatientId" FROM ins_patient
                                                    )
                                                    SELECT
                                                        ip."PatientCode",
                                                        pe."PersonCode",
                                                        pe."Email",
                                                        pe."Names",
                                                        pe."Surnames",
                                                        ip."MedicalRecordNumber",
                                                        ip."PasswordHash",
                                                        ip."IsActive"
                                                    FROM ins_patient ip
                                                    JOIN ins_person pe ON pe."PersonId" = ip."PersonId"
                                                    """;

    internal const string CreatePatientForPerson = $"""
                                                    WITH {MrnCte},
                                                    ins_patient AS (
                                                        INSERT INTO "Patient" ("PersonId", "MedicalRecordNumber", "PasswordHash")
                                                        SELECT p."PersonId", mrn.value, @PasswordHash
                                                        FROM "Person" p, mrn
                                                        WHERE p."PersonCode" = @PersonCode
                                                        RETURNING *
                                                    ),
                                                    ins_token AS (
                                                        INSERT INTO "PatientRefreshToken" ("PatientId")
                                                        SELECT "PatientId" FROM ins_patient
                                                    )
                                                    SELECT
                                                        ip."PatientCode",
                                                        pe."PersonCode",
                                                        pe."Email",
                                                        pe."Names",
                                                        pe."Surnames",
                                                        ip."MedicalRecordNumber",
                                                        ip."PasswordHash",
                                                        ip."IsActive"
                                                    FROM ins_patient ip
                                                    JOIN "Person" pe ON pe."PersonId" = ip."PersonId"
                                                    """;

    internal const string AddCredentials = """
                                           WITH updated AS (
                                               UPDATE "Patient"
                                               SET "PasswordHash" = @PasswordHash
                                               WHERE "PersonId"     = (SELECT "PersonId" FROM "Person" WHERE "PersonCode" = @PersonCode)
                                                 AND "PasswordHash" IS NULL
                                                 AND "IsActive"     = TRUE
                                               RETURNING *
                                           ),
                                           upsert_token AS (
                                               INSERT INTO "PatientRefreshToken" ("PatientId")
                                               SELECT "PatientId" FROM updated
                                               ON CONFLICT ("PatientId") DO NOTHING
                                           )
                                           SELECT
                                               up."PatientCode",
                                               pe."PersonCode",
                                               pe."Email",
                                               pe."Names",
                                               pe."Surnames",
                                               up."MedicalRecordNumber",
                                               up."PasswordHash",
                                               up."IsActive"
                                           FROM updated up
                                           JOIN "Person" pe ON pe."PersonId" = up."PersonId"
                                           """;

    internal const string UpdateRefreshToken = """
                                               INSERT INTO "PatientRefreshToken" ("PatientId", "TokenHash", "ExpiresAt", "CreatedAt")
                                               VALUES (
                                                   (SELECT "PatientId" FROM "Patient" WHERE "PatientCode" = @PatientCode),
                                                   @TokenHash,
                                                   @ExpiresAt,
                                                   NOW()
                                               )
                                               ON CONFLICT ("PatientId") DO UPDATE
                                               SET "TokenHash" = @TokenHash,
                                                   "ExpiresAt" = @ExpiresAt,
                                                   "CreatedAt" = NOW()
                                               """;

    internal const string ClearRefreshToken = """
                                              UPDATE "PatientRefreshToken"
                                              SET "TokenHash" = NULL,
                                                  "ExpiresAt" = NULL
                                              WHERE "PatientId" = (SELECT "PatientId" FROM "Patient" WHERE "PatientCode" = @PatientCode)
                                              """;
}