namespace Infrastructure.Core.Services.Mfa;

internal static class MfaRepositorySql
{
    internal const string GetByUserCode = """
                                          SELECT
                                              m."SecretEncrypted",
                                              (m."ConfirmedAt" IS NOT NULL) AS "IsConfirmed",
                                              m."LastUsedStep"
                                          FROM "UserMfa" m
                                          INNER JOIN "User" u ON u."UserId" = m."UserId"
                                          WHERE u."UserCode" = @UserCode
                                          """;

    /// <summary>Inserts or replaces a pending row; a confirmed row is left untouched (0 rows affected).</summary>
    internal const string UpsertPending = """
                                          INSERT INTO "UserMfa" ("UserId", "SecretEncrypted")
                                          SELECT u."UserId", @Secret FROM "User" u WHERE u."UserCode" = @UserCode
                                          ON CONFLICT ("UserId") DO UPDATE
                                              SET "SecretEncrypted" = EXCLUDED."SecretEncrypted",
                                                  "LastUsedStep"    = NULL,
                                                  "CreatedAt"       = NOW()
                                              WHERE "UserMfa"."ConfirmedAt" IS NULL
                                          """;

    internal const string Confirm = """
                                    UPDATE "UserMfa" m
                                    SET "ConfirmedAt" = NOW(), "LastUsedStep" = @Step
                                    FROM "User" u
                                    WHERE u."UserId" = m."UserId"
                                      AND u."UserCode" = @UserCode
                                      AND m."ConfirmedAt" IS NULL
                                    """;

    internal const string AdvanceStep = """
                                        UPDATE "UserMfa" m
                                        SET "LastUsedStep" = @Step
                                        FROM "User" u
                                        WHERE u."UserId" = m."UserId"
                                          AND u."UserCode" = @UserCode
                                          AND m."ConfirmedAt" IS NOT NULL
                                          AND (m."LastUsedStep" IS NULL OR m."LastUsedStep" < @Step)
                                        """;

    internal const string Delete = """
                                   DELETE FROM "UserMfa" m
                                   USING "User" u
                                   WHERE u."UserId" = m."UserId" AND u."UserCode" = @UserCode
                                   """;
}
