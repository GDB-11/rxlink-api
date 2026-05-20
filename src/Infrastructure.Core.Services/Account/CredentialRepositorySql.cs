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
        u."UserId",
        u."UserCode",
        u."PersonId",
        u."RoleId",
        u."SpecialtyId",
        u."Username",
        u."Email",
        u."PasswordHash",
        u."LicenseNumber",
        u."IsActive",
        u."CreatedAt",
        u."LastAccess",
        p."Names",
        p."Surnames"
        """;

    private const string UserFromJoin = """
        FROM public."User"   u
        INNER JOIN public."Person" p ON p."PersonId" = u."PersonId"
        """;

    // Only active, non-deleted users are valid credentials.
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
        SELECT {UserProjection}
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
        INSERT INTO "RefreshToken" ("UserId", "TokenHash", "ExpiresAt")
        VALUES (
            (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode),
            @TokenHash,
            @ExpiresAt
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
}