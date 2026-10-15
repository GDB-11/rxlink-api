namespace Infrastructure.Core.Services.PrivacyNotice;

/// <summary>
/// SQL used exclusively by <see cref="PrivacyNoticeRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class PrivacyNoticeRepositorySql
{
    private const string Projection = """
                                      n."PrivacyNoticeCode", n."Version", n."Title", n."Body",
                                      n."EffectiveFrom", n."PublishedAt"
                                      """;

    internal const string Publish = $"""
                                     WITH ins AS (
                                         INSERT INTO "PrivacyNotice" ("Version", "Title", "Body", "EffectiveFrom", "PublishedBy")
                                         SELECT COALESCE((SELECT MAX("Version") FROM "PrivacyNotice"), 0) + 1,
                                                @Title, @Body, @EffectiveFrom, u."UserId"
                                         FROM "User" u
                                         WHERE u."UserCode" = @PublishedByUserCode
                                         RETURNING *
                                     )
                                     SELECT {Projection} FROM ins n
                                     """;

    internal const string GetPage = $"""
                                     SELECT {Projection}, COUNT(*) OVER() AS "TotalCount"
                                     FROM "PrivacyNotice" n
                                     ORDER BY n."Version" DESC
                                     OFFSET @Offset LIMIT @Limit
                                     """;

    internal const string GetCurrent = $"""
                                        SELECT {Projection}
                                        FROM "PrivacyNotice" n
                                        WHERE n."EffectiveFrom" <= (NOW() AT TIME ZONE 'America/Lima')::date
                                        ORDER BY n."Version" DESC
                                        LIMIT 1
                                        """;

    internal const string GetAcknowledgments = """
                                               SELECT n."PrivacyNoticeCode", n."Version", n."Title", a."AcknowledgedAt",
                                                      rp."Names" || ' ' || rp."Surnames" AS "RecordedByName"
                                               FROM "PatientPrivacyNoticeAck" a
                                               JOIN "Patient" pat ON pat."PatientId" = a."PatientId"
                                               JOIN "PrivacyNotice" n ON n."PrivacyNoticeId" = a."PrivacyNoticeId"
                                               LEFT JOIN "User" ru ON ru."UserId" = a."RecordedBy"
                                               LEFT JOIN "Person" rp ON rp."PersonId" = ru."PersonId"
                                               WHERE pat."PatientCode" = @PatientCode
                                               ORDER BY a."AcknowledgedAt" DESC, a."PatientPrivacyNoticeAckId" DESC
                                               """;

    internal const string AcknowledgeCurrent = """
                                               WITH current_notice AS (
                                                   SELECT "PrivacyNoticeId"
                                                   FROM "PrivacyNotice"
                                                   WHERE "EffectiveFrom" <= (NOW() AT TIME ZONE 'America/Lima')::date
                                                   ORDER BY "Version" DESC
                                                   LIMIT 1
                                               )
                                               INSERT INTO "PatientPrivacyNoticeAck" ("PatientId", "PrivacyNoticeId", "RecordedBy")
                                               SELECT p."PatientId", c."PrivacyNoticeId",
                                                      (SELECT u."UserId" FROM "User" u WHERE u."UserCode" = @RecordedByUserCode)
                                               FROM "Patient" p
                                               CROSS JOIN current_notice c
                                               WHERE p."PatientCode" = @PatientCode AND p."IsActive" AND p."DeletedAt" IS NULL
                                               ON CONFLICT ("PatientId", "PrivacyNoticeId") DO NOTHING
                                               """;
}
