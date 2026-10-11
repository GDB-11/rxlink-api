namespace Infrastructure.Core.Interfaces.Activity;

/// <summary>
/// Durable access log (architecture doc §12.3): records every read of patient-identifiable data as a
/// <c>Record.Accessed</c> <c>ActivityEvent</c>. The insert is awaited by the caller before the response
/// is returned, so a failure to log fails the read.
/// </summary>
public interface IRecordAccessLogger
{
    /// <param name="resourceType">Kind of data read, see <see cref="AccessResourceTypes"/>.</param>
    /// <param name="patientCode">Patient whose data was read.</param>
    /// <param name="resourceCode">Specific record read (prescription, appointment…), if any.</param>
    /// <param name="viaBreakGlass">The caller was outside the care team and read under a break-glass grant.</param>
    Task LogAsync(string resourceType, Guid patientCode, Guid? resourceCode = null, bool viaBreakGlass = false);
}

public static class AccessResourceTypes
{
    public const string PatientProfile = "PatientProfile";
    public const string Diagnostics = "Diagnostics";
    public const string Prescription = "Prescription";
    public const string Appointment = "Appointment";
    public const string Appointments = "Appointments";
}
