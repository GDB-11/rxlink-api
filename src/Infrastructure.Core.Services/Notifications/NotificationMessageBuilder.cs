using System.Globalization;
using System.Text.Json;
using Infrastructure.Core.Interfaces.Notifications;

namespace Infrastructure.Core.Services.Notifications;

/// <summary>
/// Renders the message text from a template type and its stored parameters. Minimum necessary (HIPAA, plan §19): the
/// text never names the patient, a doctor, a diagnosis or a medication — only that something is due, and when.
/// </summary>
public static class NotificationMessageBuilder
{
    private static readonly TimeZoneInfo Lima = ResolveLima();

    public static (string Subject, string Body)? Build(string notificationType, string parametersJson)
    {
        using JsonDocument parameters = JsonDocument.Parse(parametersJson);
        JsonElement root = parameters.RootElement;

        switch (notificationType)
        {
            case NotificationTypes.AppointmentReminder
                when root.TryGetProperty("scheduledAt", out JsonElement at) && at.TryGetDateTimeOffset(out DateTimeOffset when):
                DateTime local = TimeZoneInfo.ConvertTime(when, Lima).DateTime;
                return ("Recordatorio de cita",
                    $"Recordatorio: tiene una cita el {local:dd/MM/yyyy} a las {local:HH:mm}. " +
                    "Si no puede asistir, comuníquese con el centro.");

            case NotificationTypes.PrescriptionExpiring
                when root.TryGetProperty("validUntil", out JsonElement until)
                     && DateOnly.TryParse(until.GetString(), CultureInfo.InvariantCulture, out DateOnly date):
                return ("Aviso de vencimiento",
                    $"Aviso: una de sus recetas vence el {date:dd/MM/yyyy}. Consulte con su centro de salud.");

            default:
                return null;
        }
    }

    private static TimeZoneInfo ResolveLima()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("Lima", TimeSpan.FromHours(-5), "Lima", "Lima"); }
    }
}
