using System.Data;
using Dapper;
using Infrastructure.Core.Interfaces.Notifications;

namespace Infrastructure.Core.Services.Notifications;

public sealed class NotificationOutboxRepository(IDbConnection connection) : BaseDatabaseService, INotificationOutboxRepository
{
    /// <summary>
    /// <c>DedupKey</c> keeps this idempotent: running it every tick creates each row once. The stored parameters are
    /// dates only — never a diagnosis, medication or name.
    /// </summary>
    internal const string EnqueueAppointmentReminders = """
        INSERT INTO "NotificationOutbox" ("NotificationType", "Channel", "PatientId", "EntityType", "EntityCode", "Parameters", "DedupKey")
        SELECT 'AppointmentReminder', c.channel, a."PatientId", 'Appointment', a."AppointmentCode",
               jsonb_build_object('scheduledAt', a."ScheduledAt"),
               'appointment-reminder:' || a."AppointmentCode" || ':' || c.channel
        FROM "Appointment" a
        JOIN "AppointmentStatus" s ON s."AppointmentStatusId" = a."AppointmentStatusId"
        CROSS JOIN unnest(@Channels) AS c(channel)
        WHERE s."Name" = 'Confirmado'
          AND a."ScheduledAt" > NOW()
          AND a."ScheduledAt" <= NOW() + make_interval(hours => @WindowHours)
        ON CONFLICT ("DedupKey") DO NOTHING
        """;

    internal const string EnqueuePrescriptionNotices = """
        INSERT INTO "NotificationOutbox" ("NotificationType", "Channel", "PatientId", "EntityType", "EntityCode", "Parameters", "DedupKey")
        SELECT 'PrescriptionExpiring', c.channel, p."PatientId", 'Prescription', p."PrescriptionCode",
               jsonb_build_object('validUntil', p."ValidUntil"),
               'prescription-expiring:' || p."PrescriptionCode" || ':' || c.channel
        FROM "Prescription" p
        JOIN "PrescriptionStatus" s ON s."PrescriptionStatusId" = p."PrescriptionStatusId"
        CROSS JOIN unnest(@Channels) AS c(channel)
        WHERE s."Name" = 'Activo'
          AND p."DeletedAt" IS NULL
          AND p."ValidUntil" = (NOW() AT TIME ZONE 'America/Lima')::date + 1
        ON CONFLICT ("DedupKey") DO NOTHING
        """;

    /// <summary>The lease (<c>NextAttemptAt</c> pushed out) is what makes a claim safe across instances and crashes.</summary>
    internal const string ClaimBatch = """
        WITH due AS (
            SELECT "NotificationId"
            FROM "NotificationOutbox"
            WHERE "Status" = 'Pending' AND "NextAttemptAt" <= NOW() AND "Channel" = ANY(@Channels)
            ORDER BY "NextAttemptAt"
            LIMIT @BatchSize
            FOR UPDATE SKIP LOCKED
        ),
        claimed AS (
            UPDATE "NotificationOutbox" n
            SET "Attempts" = n."Attempts" + 1,
                "NextAttemptAt" = NOW() + make_interval(secs => @LeaseSeconds)
            FROM due
            WHERE n."NotificationId" = due."NotificationId"
            RETURNING n.*
        )
        SELECT c."NotificationId", c."NotificationCode", c."NotificationType", c."Channel",
               COALESCE(pref."AlternativeEmail", NULLIF(btrim(pe."Email"), '')) AS "Email",
               COALESCE(pref."AlternativePhone", NULLIF(btrim(pe."Phone"), '')) AS "Phone",
               pa."IsActive" AND pa."DeletedAt" IS NULL AS "PatientActive",
               CASE c."Channel" WHEN 'Email' THEN COALESCE(pref."AllowEmail", TRUE)
                                WHEN 'Sms' THEN COALESCE(pref."AllowSms", TRUE)
                                ELSE TRUE END AS "ChannelAllowed",
               c."Parameters"::text AS "Parameters", c."Attempts"
        FROM claimed c
        JOIN "Patient" pa ON pa."PatientId" = c."PatientId"
        JOIN "Person" pe ON pe."PersonId" = pa."PersonId"
        LEFT JOIN "PatientCommunicationPreference" pref ON pref."PatientId" = pa."PatientId"
        ORDER BY c."NotificationId"
        """;

    internal const string MarkSent = """
        UPDATE "NotificationOutbox" SET "Status" = 'Sent', "SentAt" = NOW(), "LastErrorType" = NULL
        WHERE "NotificationId" = @Id
        """;

    internal const string MarkFailed = """
        UPDATE "NotificationOutbox"
        SET "LastErrorType" = @ErrorType,
            "Status" = CASE WHEN @Final THEN 'Failed' ELSE 'Pending' END,
            "NextAttemptAt" = NOW() + make_interval(secs => @RetrySeconds)
        WHERE "NotificationId" = @Id
        """;

    internal const string MarkSkipped = """
        UPDATE "NotificationOutbox" SET "Status" = 'Skipped', "LastErrorType" = @Reason WHERE "NotificationId" = @Id
        """;

    public async Task<int> EnqueueDueAsync(IReadOnlyCollection<string> channels, int appointmentWindowHours)
    {
        if (channels.Count == 0) return 0;
        var args = new { Channels = channels.ToArray(), WindowHours = appointmentWindowHours };
        return await ExecuteWithTransactionAsync(connection, async transaction =>
            await connection.ExecuteAsync(EnqueueAppointmentReminders, args, transaction) +
            await connection.ExecuteAsync(EnqueuePrescriptionNotices, args, transaction));
    }

    public async Task<IReadOnlyList<PendingNotification>> ClaimBatchAsync(
        IReadOnlyCollection<string> channels, int batchSize, TimeSpan lease)
    {
        if (channels.Count == 0) return [];
        return await ExecuteWithTransactionAsync<IReadOnlyList<PendingNotification>>(connection, async transaction =>
            (await connection.QueryAsync<PendingNotification>(ClaimBatch,
                new { Channels = channels.ToArray(), BatchSize = batchSize, LeaseSeconds = lease.TotalSeconds }, transaction)).AsList());
    }

    public Task MarkSentAsync(long notificationId) =>
        connection.ExecuteAsync(MarkSent, new { Id = notificationId });

    public Task MarkFailedAsync(long notificationId, string errorType, TimeSpan retryAfter, bool final) =>
        connection.ExecuteAsync(MarkFailed,
            new { Id = notificationId, ErrorType = errorType, Final = final, RetrySeconds = retryAfter.TotalSeconds });

    public Task MarkSkippedAsync(long notificationId, string reason) =>
        connection.ExecuteAsync(MarkSkipped, new { Id = notificationId, Reason = reason });
}
