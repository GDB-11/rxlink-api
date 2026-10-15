using Infrastructure.Core.Interfaces.Notifications;


namespace RxLinkApi.DependencyInjection;

/// <summary>
/// Placeholder provider: delivers nothing and logs only the channel. For development and for deployments whose real
/// provider (and its BAA-equivalent agreement) is not chosen yet. The recipient and the text are never logged.
/// </summary>
public sealed class LogOnlyNotificationChannel(string channel, ILogger<LogOnlyNotificationChannel> logger) : INotificationChannel
{
    public string Channel { get; } = channel;

    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("Notification accepted by log-only provider on channel {Channel}", Channel);
        return Task.CompletedTask;
    }
}
