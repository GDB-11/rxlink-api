namespace Infrastructure.Core.Interfaces.Notifications;

public static class NotificationTypes
{
    public const string AppointmentReminder = "AppointmentReminder";
    public const string PrescriptionExpiring = "PrescriptionExpiring";
}

public static class NotificationChannels
{
    public const string Email = "Email";
    public const string Sms = "Sms";
}

/// <summary>
/// A claimed outbox row with the recipient's contact data resolved at send time: the patient's alternative contact
/// (confidential communications) when set, otherwise the person record. <see cref="ChannelAllowed"/> is <c>false</c>
/// when the patient restricted this channel. <see cref="Parameters"/> is the raw JSON stored with the row (only
/// non-clinical values such as a date).
/// </summary>
public sealed record PendingNotification(
    long NotificationId,
    Guid NotificationCode,
    string NotificationType,
    string Channel,
    string? Email,
    string? Phone,
    bool PatientActive,
    bool ChannelAllowed,
    string Parameters,
    int Attempts);

public interface INotificationOutboxRepository
{
    /// <summary>
    /// Enqueues reminders for <c>Confirmado</c> appointments in the next <paramref name="appointmentWindowHours"/> hours and
    /// notices for <c>Activo</c> prescriptions that expire tomorrow (America/Lima), once per entity and channel.
    /// Returns how many rows were created.
    /// </summary>
    Task<int> EnqueueDueAsync(IReadOnlyCollection<string> channels, int appointmentWindowHours);

    /// <summary>
    /// Claims up to <paramref name="batchSize"/> due rows for <paramref name="channels"/>: bumps <c>Attempts</c> and pushes
    /// <c>NextAttemptAt</c> out by <paramref name="lease"/>, so a crashed worker's rows come back after the lease.
    /// </summary>
    Task<IReadOnlyList<PendingNotification>> ClaimBatchAsync(IReadOnlyCollection<string> channels, int batchSize, TimeSpan lease);

    Task MarkSentAsync(long notificationId);

    /// <summary>Records a failed send: retried after <paramref name="retryAfter"/>, or <c>Failed</c> once <paramref name="final"/>.</summary>
    Task MarkFailedAsync(long notificationId, string errorType, TimeSpan retryAfter, bool final);

    /// <summary>Terminal state for a row that cannot be sent (no contact data, inactive patient).</summary>
    Task MarkSkippedAsync(long notificationId, string reason);
}
