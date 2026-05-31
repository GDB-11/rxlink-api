namespace Infrastructure.Core.Services.Person;

internal static class PersonRepositorySql
{
    internal const string GetPage = """
        SELECT
            p."PersonCode",
            p."Names",
            p."Surnames",
            p."BirthDate",
            s."SexCode",
            s."Name"  AS "SexName",
            p."Phone",
            p."AlternativePhone",
            p."Email",
            p."Address",
            p."EmergencyContactName",
            p."EmergencyContactPhone",
            doc."DocumentTypeCode",
            doc."DocumentTypeName",
            doc."DocumentNumber",
            COUNT(*) OVER () AS "TotalCount"
        FROM "Person" p
        JOIN "Sex" s ON s."SexId" = p."SexId"
        LEFT JOIN LATERAL (
            SELECT dt."DocumentTypeCode",
                   dt."Name"   AS "DocumentTypeName",
                   pd."Number" AS "DocumentNumber"
            FROM   "PersonDocument" pd
            JOIN   "DocumentType" dt ON dt."DocumentTypeId" = pd."DocumentTypeId"
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

    internal const string Insert = """
        WITH ins AS (
            INSERT INTO "Person" (
                "Names", "Surnames", "BirthDate", "SexId",
                "Phone", "AlternativePhone", "Email",
                "Address", "EmergencyContactName", "EmergencyContactPhone"
            )
            SELECT
                @Names, @Surnames, @BirthDate, s."SexId",
                @Phone, @AlternativePhone, @Email,
                @Address, @EmergencyContactName, @EmergencyContactPhone
            FROM "Sex" s
            WHERE s."SexCode" = @SexCode
            RETURNING *
        ),
        doc_ins AS (
            INSERT INTO "PersonDocument" ("PersonId", "DocumentTypeId", "Number")
            SELECT i."PersonId", dt."DocumentTypeId", @DocumentNumber
            FROM ins i
            JOIN "DocumentType" dt ON dt."DocumentTypeCode" = @DocumentTypeCode
            RETURNING "PersonId", "DocumentTypeId", "Number"
        )
        SELECT
            ins."PersonCode",
            ins."Names",
            ins."Surnames",
            ins."BirthDate",
            s."SexCode",
            s."Name"  AS "SexName",
            ins."Phone",
            ins."AlternativePhone",
            ins."Email",
            ins."Address",
            ins."EmergencyContactName",
            ins."EmergencyContactPhone",
            dt."DocumentTypeCode",
            dt."Name"  AS "DocumentTypeName",
            doc_ins."Number" AS "DocumentNumber",
            0 AS "TotalCount"
        FROM ins
        JOIN "Sex" s ON s."SexId" = ins."SexId"
        JOIN doc_ins ON doc_ins."PersonId" = ins."PersonId"
        JOIN "DocumentType" dt ON dt."DocumentTypeId" = doc_ins."DocumentTypeId"
        """;

    internal const string Update = """
        WITH upd AS (
            UPDATE "Person"
            SET
                "Names"                 = @Names,
                "Surnames"              = @Surnames,
                "BirthDate"             = @BirthDate,
                "SexId"                 = (SELECT "SexId" FROM "Sex" WHERE "SexCode" = @SexCode),
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
            INSERT INTO "PersonDocument" ("PersonId", "DocumentTypeId", "Number")
            SELECT u."PersonId", dt."DocumentTypeId", @DocumentNumber
            FROM upd u
            JOIN "DocumentType" dt ON dt."DocumentTypeCode" = @DocumentTypeCode
            RETURNING "PersonId", "DocumentTypeId", "Number"
        )
        SELECT
            upd."PersonCode",
            upd."Names",
            upd."Surnames",
            upd."BirthDate",
            s."SexCode",
            s."Name"  AS "SexName",
            upd."Phone",
            upd."AlternativePhone",
            upd."Email",
            upd."Address",
            upd."EmergencyContactName",
            upd."EmergencyContactPhone",
            dt."DocumentTypeCode",
            dt."Name"  AS "DocumentTypeName",
            doc_ins."Number" AS "DocumentNumber",
            0 AS "TotalCount"
        FROM upd
        JOIN "Sex" s ON s."SexId" = upd."SexId"
        JOIN doc_ins ON doc_ins."PersonId" = upd."PersonId"
        JOIN "DocumentType" dt ON dt."DocumentTypeId" = doc_ins."DocumentTypeId"
        """;
}
