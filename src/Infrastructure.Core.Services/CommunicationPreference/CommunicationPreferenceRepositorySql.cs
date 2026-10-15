namespace Infrastructure.Core.Services.CommunicationPreference;

/// <summary>
/// SQL used exclusively by <see cref="CommunicationPreferenceRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class CommunicationPreferenceRepositorySql
{
    /// <summary>A patient without a row gets the defaults (everything allowed, no alternative contact).</summary>
    internal const string Get = """
                                SELECT p."PatientCode",
                                       COALESCE(c."AllowEmail", TRUE) AS "AllowEmail",
                                       COALESCE(c."AllowSms", TRUE) AS "AllowSms",
                                       c."AlternativeEmail", c."AlternativePhone", c."UpdatedAt"
                                FROM "Patient" p
                                LEFT JOIN "PatientCommunicationPreference" c ON c."PatientId" = p."PatientId"
                                WHERE p."PatientCode" = @PatientCode AND p."IsActive" AND p."DeletedAt" IS NULL
                                """;

    internal const string Upsert = """
                                   WITH saved AS (
                                       INSERT INTO "PatientCommunicationPreference"
                                           ("PatientId", "AllowEmail", "AllowSms", "AlternativeEmail", "AlternativePhone")
                                       SELECT p."PatientId", @AllowEmail, @AllowSms, @AlternativeEmail, @AlternativePhone
                                       FROM "Patient" p
                                       WHERE p."PatientCode" = @PatientCode AND p."IsActive" AND p."DeletedAt" IS NULL
                                       ON CONFLICT ("PatientId") DO UPDATE
                                       SET "AllowEmail" = EXCLUDED."AllowEmail",
                                           "AllowSms" = EXCLUDED."AllowSms",
                                           "AlternativeEmail" = EXCLUDED."AlternativeEmail",
                                           "AlternativePhone" = EXCLUDED."AlternativePhone",
                                           "UpdatedAt" = NOW()
                                       RETURNING *
                                   )
                                   SELECT @PatientCode AS "PatientCode", s."AllowEmail", s."AllowSms",
                                          s."AlternativeEmail", s."AlternativePhone", s."UpdatedAt"
                                   FROM saved s
                                   """;
}
