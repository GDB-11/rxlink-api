namespace Infrastructure.Core.Services.Patient;

/// <summary>
/// SQL used exclusively by <see cref="PatientRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class PatientRepositorySql
{
    /// <summary>
    /// Returns one page of patients ordered by surnames and names.
    /// <c>COUNT(*) OVER()</c> embeds the total matching count in every row.
    /// Optional <c>@Search</c> filters by names or surnames (case-insensitive).
    /// </summary>
    internal const string GetPage = """
        SELECT
            p."PatientCode",
            p."MedicalRecordNumber",
            p."IsActive",
            pe."Names",
            pe."Surnames",
            pe."BirthDate",
            pe."Phone",
            pe."AlternativePhone",
            pe."Email",
            pe."Address",
            pe."EmergencyContactName",
            pe."EmergencyContactPhone",
            COUNT(*) OVER () AS "TotalCount"
        FROM "Patient" p
        INNER JOIN "Person" pe ON pe."PersonId" = p."PersonId"
        WHERE (@Search IS NULL
               OR pe."Names"    ILIKE '%' || @Search || '%'
               OR pe."Surnames" ILIKE '%' || @Search || '%')
        ORDER BY pe."Surnames", pe."Names"
        LIMIT @Limit OFFSET @Offset
        """;

    /// <summary>
    /// Inserts a Person and a Patient in one CTE chain and returns the full flat row.
    /// </summary>
    internal const string Insert = """
        WITH ins_person AS (
            INSERT INTO "Person" (
                "Names", "Surnames", "BirthDate", "SexId",
                "Phone", "AlternativePhone", "Email",
                "Address", "EmergencyContactName", "EmergencyContactPhone"
            )
            VALUES (
                @Names, @Surnames, @BirthDate, 1,
                @Phone, @AlternativePhone, @Email,
                @Address, @EmergencyContactName, @EmergencyContactPhone
            )
            RETURNING *
        ),
        ins_patient AS (
            INSERT INTO "Patient" ("PersonId", "MedicalRecordNumber")
            SELECT "PersonId", @MedicalRecordNumber FROM ins_person
            RETURNING *
        )
        SELECT
            ins_patient."PatientCode",
            ins_patient."MedicalRecordNumber",
            ins_patient."IsActive",
            ins_person."Names",
            ins_person."Surnames",
            ins_person."BirthDate",
            ins_person."Phone",
            ins_person."AlternativePhone",
            ins_person."Email",
            ins_person."Address",
            ins_person."EmergencyContactName",
            ins_person."EmergencyContactPhone",
            0 AS "TotalCount"
        FROM ins_patient
        CROSS JOIN ins_person
        """;

    /// <summary>
    /// Updates Person and Patient for an active patient identified by code.
    /// Returns no rows when the code does not match an active record.
    /// </summary>
    internal const string Update = """
        WITH upd_patient AS (
            SELECT p."PatientId", p."PersonId", p."PatientCode"
            FROM "Patient" p
            WHERE p."PatientCode" = @Code
              AND p."IsActive" = TRUE
        ),
        upd_person AS (
            UPDATE "Person" pe
            SET
                "Names"                 = @Names,
                "Surnames"              = @Surnames,
                "BirthDate"             = @BirthDate,
                "Phone"                 = @Phone,
                "AlternativePhone"      = @AlternativePhone,
                "Email"                 = @Email,
                "Address"               = @Address,
                "EmergencyContactName"  = @EmergencyContactName,
                "EmergencyContactPhone" = @EmergencyContactPhone
            FROM upd_patient
            WHERE pe."PersonId" = upd_patient."PersonId"
            RETURNING pe.*
        ),
        upd_pat AS (
            UPDATE "Patient" p
            SET "MedicalRecordNumber" = @MedicalRecordNumber
            FROM upd_patient
            WHERE p."PatientId" = upd_patient."PatientId"
            RETURNING p.*
        )
        SELECT
            upd_pat."PatientCode",
            upd_pat."MedicalRecordNumber",
            upd_pat."IsActive",
            upd_person."Names",
            upd_person."Surnames",
            upd_person."BirthDate",
            upd_person."Phone",
            upd_person."AlternativePhone",
            upd_person."Email",
            upd_person."Address",
            upd_person."EmergencyContactName",
            upd_person."EmergencyContactPhone",
            0 AS "TotalCount"
        FROM upd_pat
        CROSS JOIN upd_person
        """;

    /// <summary>
    /// Soft-deletes an active patient.
    /// Affects 0 rows when the code does not match an active record.
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
    /// Affects 0 rows when the code does not match an inactive record.
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
}
