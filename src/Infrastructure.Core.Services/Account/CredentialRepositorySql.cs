namespace Infrastructure.Core.Services.Account;

/// <summary>
/// Raw SQL statements used exclusively by <see cref="CredentialRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase naming convention
/// defined in the PostgreSQL DDL.
/// </summary>
internal static class CredentialRepositorySql
{
    // ── Shared column projection ──────────────────────────────────────────────
    // Reused by every SELECT that returns a User DTO.
    private const string UserProjection = """
                                          u."UserCode",
                                          u."PersonId",
                                          u."RoleId",
                                          r."Name" AS "RoleName",
                                          u."SpecialtyCode",
                                          u."Username",
                                          u."Email",
                                          u."PasswordHash",
                                          u."LicenseNumber",
                                          u."IsActive",
                                          u."CreatedAt",
                                          u."LastAccess",
                                          EXISTS (SELECT 1 FROM public."UserMfa" m
                                                  WHERE m."UserId" = u."UserId" AND m."ConfirmedAt" IS NOT NULL) AS "MfaEnabled",
                                          lk."LockedUntil",
                                          p."Names",
                                          p."Surnames"
                                          """;

    private const string UserFromJoin = """
                                        FROM public."User"   u
                                        INNER JOIN public."Person" p ON p."PersonId" = u."PersonId"
                                        INNER JOIN public."Role" r ON u."RoleId" = r."RoleId"
                                        LEFT JOIN public."UserLockout" lk ON lk."UserId" = u."UserId"
                                        """;

    private const string ActiveUserFilter = """
                                            u."IsActive"  = TRUE
                                            AND u."DeletedAt" IS NULL
                                            """;

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Fetch an active user by their unique username.</summary>
    internal const string GetByUsername = $"""
                                           SELECT {UserProjection}
                                           {UserFromJoin}
                                           WHERE u."Username" = @Username
                                             AND {ActiveUserFilter}
                                           """;

    /// <summary>Fetch an active user by their public UUID (<c>UserCode</c>).</summary>
    internal const string GetByCode = $"""
                                       SELECT {UserProjection}
                                       {UserFromJoin}
                                       WHERE u."UserCode" = @UserCode
                                         AND {ActiveUserFilter}
                                       """;

    /// <summary>
    /// Fetch an active user through a valid (non-revoked, non-expired) refresh-token hash.
    /// The token hash is compared against <c>RefreshToken."TokenHash"</c>.
    /// </summary>
    internal const string GetByRefreshToken = $"""
                                               SELECT {UserProjection}, rt."SessionStartedAt"
                                               {UserFromJoin}
                                               INNER JOIN "RefreshToken" rt ON rt."UserId" = u."UserId"
                                               WHERE rt."TokenHash" = @TokenHash
                                                 AND rt."RevokedAt" IS NULL
                                                 AND rt."ExpiresAt" > @CurrentDate
                                                 AND {ActiveUserFilter}
                                               """;

    /// <summary>
    /// Token-rotation command executed as a single round-trip:
    /// <list type="number">
    ///   <item>Revokes every active token that belongs to the user.</item>
    ///   <item>Inserts the new hashed token.</item>
    /// </list>
    /// Affected-row count reflects the INSERT (always 1 on success).
    /// </summary>
    internal const string UpdateRefreshToken = """
                                               WITH "RevokeActive" AS (
                                                   UPDATE "RefreshToken"
                                                   SET    "RevokedAt" = @RevokedAt,
                                                          "RevokedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode)
                                                   WHERE  "UserId"    = (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode)
                                                     AND  "RevokedAt" IS NULL
                                               )
                                               INSERT INTO "RefreshToken" ("UserId", "TokenHash", "ExpiresAt", "SessionStartedAt")
                                               VALUES (
                                                   (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode),
                                                   @TokenHash,
                                                   @ExpiresAt,
                                                   @SessionStartedAt
                                               )
                                               """;

    /// <summary>
    /// Revokes every active refresh token that belongs to the user (logout).
    /// Returns the number of tokens revoked; 0 is acceptable when the user
    /// was already logged out (idempotent).
    /// </summary>
    internal const string ClearRefreshToken = """
                                              UPDATE "RefreshToken"
                                              SET    "RevokedAt" = @RevokedAt,
                                                     "RevokedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode)
                                              WHERE  "UserId"    = (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode)
                                                AND  "RevokedAt" IS NULL
                                              """;

    /// <summary>Owner of an already-revoked refresh token (token-reuse detection).</summary>
    internal const string GetRevokedRefreshTokenOwner = """
                                                        SELECT u."UserCode"
                                                        FROM "RefreshToken" rt
                                                        INNER JOIN "User" u ON u."UserId" = rt."UserId"
                                                        WHERE rt."TokenHash" = @TokenHash
                                                          AND rt."RevokedAt" IS NOT NULL
                                                        LIMIT 1
                                                        """;

    /// <summary>
    /// Registers one failed credential attempt atomically. An expired lock restarts the count at 1;
    /// reaching <c>@MaxAttempts</c> sets <c>LockedUntil</c>. Returns the resulting <c>LockedUntil</c> (NULL = not locked).
    /// </summary>
    internal const string RecordFailedLogin = """
                                              INSERT INTO "UserLockout" AS t ("UserId", "FailedCount", "LockedUntil", "LastFailedAt")
                                              SELECT u."UserId",
                                                     1,
                                                     CASE WHEN 1 >= @MaxAttempts THEN @LockedUntil END,
                                                     @Now
                                              FROM "User" u
                                              WHERE u."UserCode" = @UserCode
                                              ON CONFLICT ("UserId") DO UPDATE
                                              SET "FailedCount"  = CASE WHEN t."LockedUntil" IS NOT NULL AND t."LockedUntil" <= @Now
                                                                        THEN 1 ELSE t."FailedCount" + 1 END,
                                                  "LockedUntil"  = CASE
                                                                       WHEN t."LockedUntil" IS NOT NULL AND t."LockedUntil" > @Now
                                                                           THEN t."LockedUntil"
                                                                       WHEN (CASE WHEN t."LockedUntil" IS NOT NULL AND t."LockedUntil" <= @Now
                                                                                  THEN 1 ELSE t."FailedCount" + 1 END) >= @MaxAttempts
                                                                           THEN @LockedUntil
                                                                       ELSE NULL END,
                                                  "LastFailedAt" = @Now
                                              RETURNING "LockedUntil"
                                              """;

    /// <summary>Clears the failure counter after a completed login.</summary>
    internal const string ResetFailedLogins = """
                                              DELETE FROM "UserLockout"
                                              WHERE "UserId" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode)
                                              """;
}
