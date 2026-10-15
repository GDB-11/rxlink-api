namespace Infrastructure.Core.Services.Consent;

/// <summary>
/// SQL used exclusively by <see cref="ConsentRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class ConsentRepositorySql
{
    private const string Projection = """
                                      c."PatientConsentCode" AS "ConsentCode",
                                      pat."PatientCode",
                                      c."ConsentType",
                                      c."Description",
                                      c."GrantedAt",
                                      rp."Names" || ' ' || rp."Surnames" AS "RecordedByName",
                                      c."RecordedAt",
                                      c."RevokedAt",
                                      vp."Names" || ' ' || vp."Surnames" AS "RevokedByName",
                                      c."RevokedReason"
                                      """;

    private const string Joins = """
                                 JOIN "Patient" pat ON pat."PatientId" = c."PatientId"
                                 JOIN "User" ru ON ru."UserId" = c."RecordedBy"
                                 JOIN "Person" rp ON rp."PersonId" = ru."PersonId"
                                 LEFT JOIN "User" vu ON vu."UserId" = c."RevokedBy"
                                 LEFT JOIN "Person" vp ON vp."PersonId" = vu."PersonId"
                                 """;

    internal const string Insert = $"""
                                    WITH ins AS (
                                        INSERT INTO "PatientConsent"
                                            ("PatientId", "ConsentType", "Description", "GrantedAt", "RecordedBy")
                                        SELECT p."PatientId", @ConsentType, @Description, @GrantedAt, u."UserId"
                                        FROM "Patient" p
                                        JOIN "User" u ON u."UserCode" = @RecordedByUserCode
                                        WHERE p."PatientCode" = @PatientCode AND p."IsActive" AND p."DeletedAt" IS NULL
                                        RETURNING *
                                    )
                                    SELECT {Projection}
                                    FROM ins c
                                    {Joins}
                                    """;

    internal const string Revoke = $"""
                                    WITH upd AS (
                                        UPDATE "PatientConsent" c
                                        SET "RevokedAt" = NOW(),
                                            "RevokedReason" = @Reason,
                                            "RevokedBy" = (SELECT u."UserId" FROM "User" u WHERE u."UserCode" = @RevokedByUserCode)
                                        FROM "Patient" p
                                        WHERE p."PatientId" = c."PatientId"
                                          AND p."PatientCode" = @PatientCode
                                          AND c."PatientConsentCode" = @ConsentCode
                                          AND c."RevokedAt" IS NULL
                                        RETURNING c.*
                                    )
                                    SELECT {Projection}
                                    FROM upd c
                                    {Joins}
                                    """;

    internal const string GetPage = $"""
                                     SELECT {Projection}, COUNT(*) OVER() AS "TotalCount"
                                     FROM "PatientConsent" c
                                     {Joins}
                                     WHERE pat."PatientCode" = @PatientCode
                                     ORDER BY c."GrantedAt" DESC, c."PatientConsentId" DESC
                                     OFFSET @Offset LIMIT @Limit
                                     """;
}
