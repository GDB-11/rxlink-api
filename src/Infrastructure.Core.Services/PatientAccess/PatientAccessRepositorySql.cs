namespace Infrastructure.Core.Services.PatientAccess;

/// <summary>SQL used exclusively by <see cref="PatientAccessRepository"/>.</summary>
internal static class PatientAccessRepositorySql
{
    internal const string GetFacts = $"""
                                      SELECT
                                          COALESCE((SELECT r."CanViewClinicalData"
                                                    FROM "User" u
                                                    JOIN "Role" r ON r."RoleId" = u."RoleId"
                                                    WHERE u."UserCode" = @UserCode), FALSE) AS "CanViewClinicalData",
                                          EXISTS (SELECT 1 FROM "Patient" pat
                                                  WHERE pat."PatientCode" = @PatientCode
                                                    AND {CareTeamSql.Predicate}) AS "InCareTeam",
                                          EXISTS (SELECT 1 FROM "Patient" pat
                                                  WHERE pat."PatientCode" = @PatientCode
                                                    AND {CareTeamSql.ActiveBreakGlassPredicate}) AS "HasBreakGlass"
                                      """;

    /// <summary>
    /// Looks the patient up by exact medical record number and inserts a grant unless the patient is already
    /// in the care team. Always returns one row when the patient exists, none otherwise.
    /// </summary>
    internal const string GrantBreakGlass = $"""
                                             WITH target AS (
                                                 SELECT pat."PatientId", pat."PatientCode",
                                                        {CareTeamSql.Predicate} AS "InCareTeam"
                                                 FROM "Patient" pat
                                                 WHERE pat."MedicalRecordNumber" = @MedicalRecordNumber
                                                   AND pat."IsActive" AND pat."DeletedAt" IS NULL
                                             ),
                                             ins AS (
                                                 INSERT INTO "BreakGlassGrant" ("UserId", "PatientId", "Reason", "ExpiresAt", "IpAddress")
                                                 SELECT u."UserId", t."PatientId", @Reason,
                                                        NOW() + make_interval(mins => @TtlMinutes),
                                                        NULLIF(current_setting('app.ip', true), '')::inet
                                                 FROM target t
                                                 JOIN "User" u ON u."UserCode" = @UserCode
                                                 WHERE NOT t."InCareTeam"
                                                 RETURNING "BreakGlassGrantCode", "ExpiresAt"
                                             )
                                             SELECT t."PatientCode", t."InCareTeam",
                                                    ins."BreakGlassGrantCode" AS "GrantCode", ins."ExpiresAt"
                                             FROM target t
                                             LEFT JOIN ins ON TRUE
                                             """;

    internal const string GetBreakGlassReport = """
                                                SELECT
                                                    bg."BreakGlassGrantCode" AS "GrantCode",
                                                    u."UserCode",
                                                    u."Username",
                                                    pat."PatientCode",
                                                    pat."MedicalRecordNumber",
                                                    bg."Reason",
                                                    bg."GrantedAt",
                                                    bg."ExpiresAt"
                                                FROM "BreakGlassGrant" bg
                                                JOIN "User" u    ON u."UserId" = bg."UserId"
                                                JOIN "Patient" pat ON pat."PatientId" = bg."PatientId"
                                                WHERE bg."GrantedAt" >= @From AND bg."GrantedAt" < @ToExclusive
                                                ORDER BY bg."GrantedAt" DESC
                                                """;
}
