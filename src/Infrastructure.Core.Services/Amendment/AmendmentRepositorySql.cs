namespace Infrastructure.Core.Services.Amendment;

/// <summary>
/// SQL used exclusively by <see cref="AmendmentRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class AmendmentRepositorySql
{
    /// <summary>Row projection shared by every query; expects the alias <c>a</c> for <c>PatientAmendmentRequest</c>.</summary>
    private const string Projection = """
                                      a."AmendmentRequestCode",
                                      pat."PatientCode",
                                      pp."Names" || ' ' || pp."Surnames" AS "PatientName",
                                      pat."MedicalRecordNumber",
                                      a."TargetArea",
                                      a."RequestedChange",
                                      a."Status",
                                      a."RequestedAt",
                                      rp."Names" || ' ' || rp."Surnames" AS "ReviewedByName",
                                      a."ReviewedAt",
                                      a."DecisionReason",
                                      a."AmendmentText"
                                      """;

    private const string Joins = """
                                 JOIN "Patient" pat ON pat."PatientId" = a."PatientId"
                                 JOIN "Person" pp ON pp."PersonId" = pat."PersonId"
                                 LEFT JOIN "User" ru ON ru."UserId" = a."ReviewedBy"
                                 LEFT JOIN "Person" rp ON rp."PersonId" = ru."PersonId"
                                 """;

    internal const string Insert = $"""
                                    WITH ins AS (
                                        INSERT INTO "PatientAmendmentRequest" ("PatientId", "TargetArea", "RequestedChange")
                                        SELECT p."PatientId", @TargetArea, @RequestedChange
                                        FROM "Patient" p
                                        WHERE p."PatientCode" = @PatientCode AND p."IsActive" AND p."DeletedAt" IS NULL
                                        RETURNING *
                                    )
                                    SELECT {Projection}
                                    FROM ins a
                                    {Joins}
                                    """;

    internal const string GetByCode = $"""
                                       SELECT {Projection}
                                       FROM "PatientAmendmentRequest" a
                                       {Joins}
                                       WHERE a."AmendmentRequestCode" = @AmendmentRequestCode
                                       """;

    internal const string GetPageByPatient = $"""
                                              SELECT {Projection}, COUNT(*) OVER() AS "TotalCount"
                                              FROM "PatientAmendmentRequest" a
                                              {Joins}
                                              WHERE pat."PatientCode" = @PatientCode
                                              ORDER BY a."RequestedAt" DESC, a."AmendmentRequestId" DESC
                                              OFFSET @Offset LIMIT @Limit
                                              """;

    /// <summary>Pending first (oldest waits longest), then the decided ones, newest first.</summary>
    internal const string GetPage = $"""
                                     SELECT {Projection}, COUNT(*) OVER() AS "TotalCount"
                                     FROM "PatientAmendmentRequest" a
                                     {Joins}
                                     WHERE (@Status::varchar IS NULL OR a."Status" = @Status)
                                     ORDER BY (a."Status" = 'Pendiente') DESC,
                                              CASE WHEN a."Status" = 'Pendiente' THEN a."RequestedAt" END ASC,
                                              a."RequestedAt" DESC
                                     OFFSET @Offset LIMIT @Limit
                                     """;

    /// <summary>The <c>Status = 'Pendiente'</c> guard makes a concurrent second decision update nothing.</summary>
    internal const string Decide = $"""
                                    WITH upd AS (
                                        UPDATE "PatientAmendmentRequest"
                                        SET "Status"         = CASE WHEN @Accepted THEN 'Aceptada' ELSE 'Denegada' END,
                                            "ReviewedBy"     = (SELECT "UserId" FROM "User" WHERE "UserCode" = @ReviewerUserCode),
                                            "ReviewedAt"     = NOW(),
                                            "DecisionReason" = @Reason,
                                            "AmendmentText"  = CASE WHEN @Accepted THEN @AmendmentText END
                                        WHERE "AmendmentRequestCode" = @AmendmentRequestCode AND "Status" = 'Pendiente'
                                        RETURNING *
                                    )
                                    SELECT {Projection}
                                    FROM upd a
                                    {Joins}
                                    """;
}
