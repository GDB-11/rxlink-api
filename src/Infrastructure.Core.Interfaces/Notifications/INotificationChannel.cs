namespace Infrastructure.Core.Interfaces.Notifications;

/// <summary>A delivery provider (SMTP, SMS gateway…) for one channel. Throwing means "not delivered, retry".</summary>
public interface INotificationChannel
{
    string Channel { get; }

    /// <param name="recipient">Email address or phone number.</param>
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}
