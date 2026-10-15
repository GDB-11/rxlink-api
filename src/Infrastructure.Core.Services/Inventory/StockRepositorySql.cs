namespace Infrastructure.Core.Services.Inventory;

/// <summary>SQL used exclusively by <see cref="StockRepository"/>.</summary>
internal static class StockRepositorySql
{
    private const string DefaultClinic = """(SELECT "ClinicId" FROM "Clinic" WHERE "IsDefault" AND "IsActive")""";

    internal const string CountMovements = $"""
                                            SELECT COUNT(*) FROM "StockMovement"
                                            WHERE "ClinicId" = {DefaultClinic} AND "MedicationCode" = @MedicationCode
                                            """;

    internal const string GetMovements = $"""
                                          SELECT sm."StockMovementCode", sm."MovementType", sm."Direction", sm."Quantity",
                                                 sm."ShortfallQuantity", sm."BatchNumber", sm."ExpiresOn", sm."Reason",
                                                 u."Username", sm."OccurredAt"
                                          FROM "StockMovement" sm
                                          LEFT JOIN "User" u ON u."UserId" = sm."UserId"
                                          WHERE sm."ClinicId" = {DefaultClinic} AND sm."MedicationCode" = @MedicationCode
                                          ORDER BY sm."OccurredAt" DESC, sm."StockMovementId" DESC
                                          LIMIT @PageSize OFFSET @Offset
                                          """;

    internal const string GetReport = $"""
                                       SELECT "MedicationCode",
                                              COALESCE(SUM("Quantity") FILTER (WHERE "MovementType" = 'Entry'), 0)    AS "Entries",
                                              COALESCE(SUM("Quantity") FILTER (WHERE "MovementType" = 'Return'), 0)   AS "Returns",
                                              COALESCE(SUM("Quantity") FILTER (WHERE "MovementType" = 'Dispense'), 0) AS "Dispensed",
                                              COALESCE(SUM("Quantity") FILTER (WHERE "MovementType" = 'Loss'), 0)     AS "Losses",
                                              COALESCE(SUM("Quantity" * "Direction") FILTER (WHERE "MovementType" = 'Adjustment'), 0) AS "Adjustments",
                                              COALESCE(SUM("ShortfallQuantity"), 0) AS "Shortfall"
                                       FROM "StockMovement"
                                       WHERE "ClinicId" = {DefaultClinic}
                                         AND "OccurredAt" >= @From AND "OccurredAt" < @ToExclusive
                                       GROUP BY "MedicationCode"
                                       """;

    internal const string GetDefaultClinicId = """
                                               SELECT "ClinicId" FROM "Clinic" WHERE "IsDefault" AND "IsActive"
                                               """;

    internal const string EnsureClinicStock = """
                                              INSERT INTO "ClinicStock" ("ClinicId", "MedicationCode")
                                              VALUES (@ClinicId, @MedicationCode)
                                              ON CONFLICT DO NOTHING
                                              """;

    internal const string LockClinicStock = """
                                            SELECT "StockQuantity"
                                            FROM "ClinicStock"
                                            WHERE "ClinicId" = @ClinicId AND "MedicationCode" = @MedicationCode
                                            FOR UPDATE
                                            """;

    internal const string ApplyToClinicStock = """
                                               UPDATE "ClinicStock"
                                               SET "StockQuantity" = "StockQuantity" + @Delta,
                                                   "UpdatedAt"     = NOW(),
                                                   "UpdatedBy"     = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode)
                                               WHERE "ClinicId" = @ClinicId AND "MedicationCode" = @MedicationCode
                                               """;

    internal const string InsertMovement = """
                                           WITH inserted AS (
                                               INSERT INTO "StockMovement" (
                                                   "ClinicId", "MedicationCode", "MovementType", "Direction", "Quantity",
                                                   "BatchNumber", "ExpiresOn", "Reason", "UserId"
                                               )
                                               SELECT @ClinicId, @MedicationCode, @MovementType, @Direction, @Quantity,
                                                      @BatchNumber::varchar, @ExpiresOn::date, @Reason::text,
                                                      (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode)
                                               RETURNING *
                                           )
                                           SELECT i."StockMovementCode", i."MovementType", i."Direction", i."Quantity",
                                                  i."ShortfallQuantity", i."BatchNumber", i."ExpiresOn", i."Reason",
                                                  u."Username", i."OccurredAt"
                                           FROM inserted i
                                           LEFT JOIN "User" u ON u."UserId" = i."UserId"
                                           """;
}
