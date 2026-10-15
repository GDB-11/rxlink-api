using Infrastructure.Core.Interfaces.Notifications;
using Infrastructure.Core.Services.Notifications;
using System.Data;

namespace RxLinkApi.DependencyInjection;

/// <summary>
/// Outbox worker: every <c>Notifications:IntervalMinutes</c> it enqueues what is due and delivers the pending rows through
/// the configured channel providers (<c>Notifications:Email:Provider</c> / <c>Notifications:Sms:Provider</c>, <c>None</c> by
/// default = channel off). Rows are leased, so several instances are safe. Only the channel, codes and error type names are
/// logged — never a recipient or the text.
/// </summary>
internal sealed class NotificationDispatchService : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly IReadOnlyDictionary<string, INotificationChannel> _channels;
    private readonly TimeSpan _interval;
    private readonly int _windowHours;
    private readonly int _maxAttempts;
    private readonly ILogger<NotificationDispatchService> _logger;

    public NotificationDispatchService(IServiceScopeFactory scopes, IConfiguration configuration,
        IEnumerable<INotificationChannel> channels, ILogger<NotificationDispatchService> logger)
    {
        _scopes = scopes;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Notifications:IntervalMinutes", 5)));
        _windowHours = Math.Clamp(configuration.GetValue("Notifications:AppointmentWindowHours", 24), 1, 168);
        _maxAttempts = Math.Max(1, configuration.GetValue("Notifications:MaxAttempts", 5));
        _channels = channels.ToDictionary(c => c.Channel);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channels.Count == 0)
        {
            _logger.LogInformation("Notifications disabled: no channel provider configured");
            return;
        }

        using PeriodicTimer timer = new(_interval);
        bool failing = false;
        do
        {
            try
            {
                (int queued, int sent) = await RunOnceAsync(stoppingToken);
                if (queued > 0 || sent > 0)
                    _logger.LogInformation("Notification run queued {Queued}, sent {Sent}", queued, sent);
                if (failing) _logger.LogInformation("Notification run recovered");
                failing = false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (!failing) _logger.LogError("Notification run failed: {ErrorType}", ex.GetType().Name);
                failing = true;
            }
        } while (await WaitAsync(timer, stoppingToken));
    }

    internal async Task<(int Queued, int Sent)> RunOnceAsync(CancellationToken token)
    {
        using IServiceScope scope = _scopes.CreateScope();
        INotificationOutboxRepository outbox = new NotificationOutboxRepository(
            scope.ServiceProvider.GetRequiredService<IDbConnection>());
        string[] channels = _channels.Keys.ToArray();

        int queued = await outbox.EnqueueDueAsync(channels, _windowHours);
        int sent = 0;
        IReadOnlyList<PendingNotification> batch;
        do
        {
            batch = await outbox.ClaimBatchAsync(channels, BatchSize, Lease);
            foreach (PendingNotification n in batch)
                if (await DeliverAsync(outbox, n, token)) sent++;
        } while (batch.Count == BatchSize && !token.IsCancellationRequested);

        return (queued, sent);
    }

    private async Task<bool> DeliverAsync(INotificationOutboxRepository outbox, PendingNotification n, CancellationToken token)
    {
        // Restriction (164.522): checked first, so a restricted patient is never contacted through that channel.
        if (n.PatientActive && !n.ChannelAllowed)
        {
            await outbox.MarkSkippedAsync(n.NotificationId, "PatientPreference");
            return false;
        }

        string? recipient = n.Channel == NotificationChannels.Email ? n.Email : n.Phone;
        if (!n.PatientActive || recipient is null)
        {
            await outbox.MarkSkippedAsync(n.NotificationId, n.PatientActive ? "NoContact" : "PatientInactive");
            return false;
        }

        (string Subject, string Body)? message = NotificationMessageBuilder.Build(n.NotificationType, n.Parameters);
        if (message is null)
        {
            await outbox.MarkSkippedAsync(n.NotificationId, "UnknownTemplate");
            return false;
        }

        try
        {
            await _channels[n.Channel].SendAsync(recipient, message.Value.Subject, message.Value.Body, token);
            await outbox.MarkSentAsync(n.NotificationId);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            bool final = n.Attempts >= _maxAttempts;
            // Backoff 2, 4, 8… minutes, capped at an hour.
            TimeSpan retry = TimeSpan.FromMinutes(Math.Min(60, Math.Pow(2, n.Attempts)));
            await outbox.MarkFailedAsync(n.NotificationId, ex.GetType().Name, retry, final);
            _logger.LogWarning("Notification {Code} delivery failed ({ErrorType}), attempt {Attempt}",
                n.NotificationCode, ex.GetType().Name, n.Attempts);
            return false;
        }
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken token)
    {
        try { return await timer.WaitForNextTickAsync(token); }
        catch (OperationCanceledException) { return false; }
    }
}
