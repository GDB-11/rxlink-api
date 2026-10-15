namespace Infrastructure.Core.Services.Disclosure;

/// <summary>
/// SQL used exclusively by <see cref="DisclosureRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class DisclosureRepositorySql
{
    private const string Projection = """
                                      d."DisclosureCode",
                                      pat."PatientCode",
                                      d."DisclosedAt",
                                      d."RecipientName",
                                      d."RecipientType",
                                      d."Purpose",
                                      d."Description",
                                      rp."Names" || ' ' || rp."Surnames" AS "RecordedByName",
                                      d."RecordedAt"
                                      """;

    private const string Joins = """
                                 JOIN "Patient" pat ON pat."PatientId" = d."PatientId"
                                 JOIN "User" ru ON ru."UserId" = d."RecordedBy"
                                 JOIN "Person" rp ON rp."PersonId" = ru."PersonId"
                                 """;

    internal const string Insert = $"""
                                    WITH ins AS (
                                        INSERT INTO "PatientDisclosure"
                                            ("PatientId", "DisclosedAt", "RecipientName", "RecipientType", "Purpose", "Description", "RecordedBy")
                                        SELECT p."PatientId", @DisclosedAt, @RecipientName, @RecipientType, @Purpose, @Description, u."UserId"
                                        FROM "Patient" p
                                        JOIN "User" u ON u."UserCode" = @RecordedByUserCode
                                        WHERE p."PatientCode" = @PatientCode AND p."IsActive" AND p."DeletedAt" IS NULL
                                        RETURNING *
                                    )
                                    SELECT {Projection}
                                    FROM ins d
                                    {Joins}
                                    """;

    internal const string GetPage = $"""
                                     SELECT {Projection}, COUNT(*) OVER() AS "TotalCount"
                                     FROM "PatientDisclosure" d
                                     {Joins}
                                     WHERE pat."PatientCode" = @PatientCode
                                     ORDER BY d."DisclosedAt" DESC, d."DisclosureId" DESC
                                     OFFSET @Offset LIMIT @Limit
                                     """;
}
