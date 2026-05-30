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
            COUNT(*) OVER () AS "TotalCount"
        FROM "Person" p
        JOIN "Sex" s ON s."SexId" = p."SexId"
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
            0 AS "TotalCount"
        FROM ins
        JOIN "Sex" s ON s."SexId" = ins."SexId"
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
            0 AS "TotalCount"
        FROM upd
        JOIN "Sex" s ON s."SexId" = upd."SexId"
        """;
}
