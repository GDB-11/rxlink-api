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
                                          JOIN "Person" p          ON p."PersonId"        = pd."PersonId"
                                          LEFT JOIN "Patient" pat  ON pat."PersonId"       = p."PersonId"
                                                                   AND pat."IsActive"      = TRUE
                                          WHERE pd."DocumentTypeCode" = @DocumentTypeCode
                                            AND pd."Number"           = @DocumentNumber
                                            AND p."BirthDate"         = @BirthDate
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

    internal const string CreatePersonAndPatient = $"""
                                                    WITH ins_person AS (
                                                        INSERT INTO "Person" (
                                                            "Names", "Surnames", "BirthDate", "SexCode",
                                                            "Phone", "AlternativePhone", "Email",
                                                            "Address", "EmergencyContactName", "EmergencyContactPhone"
                                                        )
                                                        VALUES (
                                                            @Names, @Surnames, @BirthDate, @SexCode,
                                                            @Phone, @AlternativePhone, @Email,
                                                            @Address, @EmergencyContactName, @EmergencyContactPhone
                                                        )
                                                        RETURNING *
                                                    ),
                                                    ins_doc AS (
                                                        INSERT INTO "PersonDocument" ("PersonId", "DocumentTypeCode", "Number")
                                                        SELECT ip."PersonId", @DocumentTypeCode, @DocumentNumber
                                                        FROM ins_person ip
                                                        RETURNING "PersonId"
                                                    ),
                                                    {MrnCte},
                                                    ins_patient AS (
                                                        INSERT INTO "Patient" ("PersonId", "MedicalRecordNumber", "PasswordHash")
                                                        SELECT ip."PersonId", mrn.value, @PasswordHash
                                                        FROM ins_person ip, mrn
                                                        RETURNING *
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

    /// <summary>Starts or continues a session: stores a new token and drops this patient's expired ones.</summary>
    internal const string StoreRefreshToken = """
                                              WITH pruned AS (
                                                  DELETE FROM "PatientRefreshToken"
                                                  WHERE "PatientId" = (SELECT "PatientId" FROM "Patient" WHERE "PatientCode" = @PatientCode)
                                                    AND "ExpiresAt" < @Now
                                              )
                                              INSERT INTO "PatientRefreshToken" ("PatientId", "FamilyCode", "TokenHash", "ExpiresAt", "CreatedAt")
                                              SELECT "PatientId", @FamilyCode, @TokenHash, @ExpiresAt, @Now
                                              FROM "Patient"
                                              WHERE "PatientCode" = @PatientCode
                                              """;

    /// <summary>
    /// Single-use rotation: marks the presented token used and stores its successor in the same family.
    /// Returns no row when the token is unknown, used, revoked, expired, or its patient is inactive.
    /// </summary>
    internal const string RotateRefreshToken = $"""
                                                WITH consumed AS (
                                                    UPDATE "PatientRefreshToken"
                                                    SET "UsedAt" = @Now
                                                    WHERE "TokenHash"  = @TokenHash
                                                      AND "UsedAt"    IS NULL
                                                      AND "RevokedAt" IS NULL
                                                      AND "ExpiresAt"  > @Now
                                                    RETURNING "PatientId", "FamilyCode"
                                                ),
                                                pruned AS (
                                                    DELETE FROM "PatientRefreshToken"
                                                    WHERE "PatientId" IN (SELECT "PatientId" FROM consumed)
                                                      AND "ExpiresAt" < @Now
                                                ),
                                                successor AS (
                                                    INSERT INTO "PatientRefreshToken" ("PatientId", "FamilyCode", "TokenHash", "ExpiresAt", "CreatedAt")
                                                    SELECT "PatientId", "FamilyCode", @NewTokenHash, @NewExpiresAt, @Now
                                                    FROM consumed
                                                )
                                                SELECT {PatientCredentialProjection}
                                                FROM consumed c
                                                JOIN "Patient" pat ON pat."PatientId" = c."PatientId"
                                                JOIN "Person" pe   ON pe."PersonId"   = pat."PersonId"
                                                WHERE pat."IsActive" = TRUE
                                                """;

    /// <summary>
    /// Reuse detection: when the presented token was already used, every live token of its family is revoked.
    /// Returns how many tokens were revoked (0 when the token is unknown, merely expired, or the family was already revoked).
    /// </summary>
    internal const string RevokeFamilyOfUsedToken = """
                                                    WITH revoked AS (
                                                        UPDATE "PatientRefreshToken"
                                                        SET "RevokedAt" = @Now
                                                        WHERE "FamilyCode" = (
                                                                SELECT "FamilyCode" FROM "PatientRefreshToken"
                                                                WHERE "TokenHash" = @TokenHash AND "UsedAt" IS NOT NULL)
                                                          AND "RevokedAt" IS NULL
                                                        RETURNING 1
                                                    )
                                                    SELECT COUNT(*) FROM revoked
                                                    """;

    internal const string RevokeAllSessions = """
                                              UPDATE "PatientRefreshToken"
                                              SET "RevokedAt" = @Now
                                              WHERE "PatientId" = (SELECT "PatientId" FROM "Patient" WHERE "PatientCode" = @PatientCode)
                                                AND "RevokedAt" IS NULL
                                              """;
}
