namespace Infrastructure.Core.Services.Report;

internal static class ReportRepositorySql
{
    /// <summary>Event counts per actor and event type in the range; unattributed events group under a NULL user.</summary>
    internal const string GetActivityByUser = """
                                              SELECT
                                                  u."UserCode",
                                                  u."Username",
                                                  e."EventType",
                                                  COUNT(*)           AS "EventCount",
                                                  MAX(e."OccurredAt") AS "LastOccurredAt"
                                              FROM "ActivityEvent" e
                                              LEFT JOIN "User" u ON u."UserId" = e."ActorUserId"
                                              WHERE e."OccurredAt" >= @From AND e."OccurredAt" < @ToExclusive
                                              GROUP BY u."UserCode", u."Username", e."EventType"
                                              ORDER BY u."Username" NULLS LAST, e."EventType"
                                              """;

    /// <summary>Audited writes whose audit context was missing (<c>UserId IS NULL</c>); background jobs mark theirs with a <c>system:</c> request id.</summary>
    internal const string GetUnattributedWrites = """
                                                  SELECT
                                                      a."TableName",
                                                      a."Action",
                                                      COUNT(*)            AS "WriteCount",
                                                      MAX(a."OccurredAt") AS "LastOccurredAt"
                                                  FROM "AuditLog" a
                                                  WHERE a."UserId" IS NULL
                                                    AND COALESCE(a."RequestId", '') NOT LIKE 'system:%'
                                                    AND a."OccurredAt" >= @From AND a."OccurredAt" < @ToExclusive
                                                  GROUP BY a."TableName", a."Action"
                                                  ORDER BY "WriteCount" DESC, a."TableName"
                                                  """;
}
