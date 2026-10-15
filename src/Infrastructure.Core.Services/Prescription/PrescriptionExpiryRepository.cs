using System.Data;
using Dapper;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Prescription;

namespace Infrastructure.Core.Services.Prescription;

public sealed class PrescriptionExpiryRepository : BaseDatabaseService, IPrescriptionExpiryRepository
{
    /// <summary>
    /// <c>ValidUntil</c> is a date: a prescription is expired once today (Lima) is after it. Rows are locked with
    /// <c>SKIP LOCKED</c> so two API instances sweeping at once never block each other or double-process a row.
    /// </summary>
    internal const string ExpireBatch = """
                                        WITH due AS (
                                            SELECT p."PrescriptionId",
                                                   CASE ps."Name" WHEN 'Dispensado' THEN 'Finalizado' ELSE 'Caducado' END AS "NewStatus"
                                            FROM "Prescription" p
                                            JOIN "PrescriptionStatus" ps ON ps."PrescriptionStatusId" = p."PrescriptionStatusId"
                                            WHERE ps."Name" IN ('Activo', 'Suspendido', 'Dispensado')
                                              AND p."ValidUntil" < (NOW() AT TIME ZONE 'America/Lima')::date
                                              AND p."DeletedAt" IS NULL
                                            ORDER BY p."PrescriptionId"
                                            LIMIT @BatchSize
                                            FOR UPDATE OF p SKIP LOCKED
                                        )
                                        UPDATE "Prescription" p
                                        SET "PrescriptionStatusId" = ns."PrescriptionStatusId",
                                            "ModifiedAt"           = NOW()
                                        FROM due
                                        JOIN "PrescriptionStatus" ns ON ns."Name" = due."NewStatus"
                                        WHERE p."PrescriptionId" = due."PrescriptionId"
                                        RETURNING p."PrescriptionCode", due."NewStatus"
                                        """;

    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;
    private readonly IActivityLogger _activityLogger;

    public PrescriptionExpiryRepository(IDbConnection connection, IUserContext userContext,
        IActivityLogger activityLogger)
    {
        _connection = connection;
        _userContext = userContext;
        _activityLogger = activityLogger;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ExpiredPrescriptionRow>> ExpireBatchAsync(int batchSize) =>
        ExecuteAuditedWriteAsync<IReadOnlyList<ExpiredPrescriptionRow>>(_connection, _userContext,
            async transaction =>
            {
                List<ExpiredPrescriptionRow> rows = (await _connection.QueryAsync<ExpiredPrescriptionRow>(
                    ExpireBatch, new { BatchSize = batchSize }, transaction)).AsList();

                foreach (ExpiredPrescriptionRow row in rows)
                {
                    bool finalized = row.NewStatus == "Finalizado";
                    await _activityLogger.LogAsync(_connection, transaction, new ActivityEvent(
                        finalized ? ActivityEventTypes.PrescriptionFinalized : ActivityEventTypes.PrescriptionExpired,
                        null, ActivityEntityTypes.Prescription, row.PrescriptionCode,
                        finalized ? "Prescription finalized (validity ended)" : "Prescription expired without dispensing"));
                }

                return rows;
            });
}
