namespace Infrastructure.Core.Interfaces.Activity;

/// <summary>Initial event catalog (architecture doc §12.2).</summary>
public static class ActivityEventTypes
{
    public const string LoginSucceeded = "Auth.LoginSucceeded";
    public const string LoginFailed = "Auth.LoginFailed";
    public const string Logout = "Auth.Logout";
    public const string PasswordChanged = "Auth.PasswordChanged";
    public const string RefreshTokenReuse = "Auth.RefreshTokenReuse";
    public const string MfaEnrolled = "Auth.MfaEnrolled";
    public const string MfaReset = "Auth.MfaReset";
    public const string MfaFailed = "Auth.MfaFailed";
    public const string AccountLocked = "Auth.AccountLocked";

    public const string PrescriptionCreated = "Prescription.Created";
    public const string PrescriptionSigned = "Prescription.Signed";
    public const string PrescriptionSuspended = "Prescription.Suspended";
    public const string PrescriptionReactivated = "Prescription.Reactivated";
    public const string PrescriptionCancelled = "Prescription.Cancelled";
    public const string PrescriptionDispensed = "Prescription.Dispensed";

    public const string AppointmentCreated = "Appointment.Created";
    public const string AppointmentCancelled = "Appointment.Cancelled";
    public const string AppointmentNoShow = "Appointment.NoShow";
    public const string AppointmentPaymentConfirmed = "Appointment.PaymentConfirmed";

    public const string RecordAccessed = "Record.Accessed";
    public const string BreakGlassAccess = "Record.BreakGlassAccess";
    public const string ReportViewed = "Report.Viewed";
}

public static class ActivityEntityTypes
{
    public const string User = "User";
    public const string Prescription = "Prescription";
    public const string Appointment = "Appointment";
    public const string Patient = "Patient";
}
