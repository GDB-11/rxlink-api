using Infrastructure.Core.Interfaces.Notifications;
using Infrastructure.Core.Services.Notifications;

namespace Application.Core.Services.Tests.Notifications;

public class NotificationMessageBuilderTests
{
    [Fact]
    public void AppointmentReminder_ShowsLimaLocalTime()
    {
        // 15:00 UTC = 10:00 in Lima (UTC-5).
        (string Subject, string Body)? message = NotificationMessageBuilder.Build(
            NotificationTypes.AppointmentReminder, """{"scheduledAt":"2026-10-15T15:00:00+00:00"}""");

        Assert.NotNull(message);
        Assert.Contains("15/10/2026", message.Value.Body);
        Assert.Contains("10:00", message.Value.Body);
    }

    [Fact]
    public void PrescriptionExpiring_ShowsDateOnly()
    {
        (string Subject, string Body)? message = NotificationMessageBuilder.Build(
            NotificationTypes.PrescriptionExpiring, """{"validUntil":"2026-10-15"}""");

        Assert.NotNull(message);
        Assert.Contains("15/10/2026", message.Value.Body);
    }

    [Theory]
    [InlineData(NotificationTypes.AppointmentReminder, """{"scheduledAt":"2026-10-15T15:00:00+00:00","diagnosis":"Asma","medication":"Salbutamol"}""")]
    [InlineData(NotificationTypes.PrescriptionExpiring, """{"validUntil":"2026-10-15","diagnosis":"Asma","medication":"Salbutamol"}""")]
    public void Message_IgnoresAnyUnexpectedParameter(string type, string json)
    {
        (string Subject, string Body)? message = NotificationMessageBuilder.Build(type, json);

        Assert.NotNull(message);
        Assert.DoesNotContain("Asma", message.Value.Subject + message.Value.Body);
        Assert.DoesNotContain("Salbutamol", message.Value.Subject + message.Value.Body);
    }

    [Theory]
    [InlineData("Unknown", "{}")]
    [InlineData(NotificationTypes.AppointmentReminder, "{}")]
    [InlineData(NotificationTypes.PrescriptionExpiring, """{"validUntil":"not a date"}""")]
    public void MissingOrInvalidParameters_YieldNoMessage(string type, string json) =>
        Assert.Null(NotificationMessageBuilder.Build(type, json));
}
