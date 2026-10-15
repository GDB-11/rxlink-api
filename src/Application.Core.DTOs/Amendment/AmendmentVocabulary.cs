namespace Application.Core.DTOs.Amendment;

public static class AmendmentTargetAreas
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        "Profile", "Allergies", "Diagnostics", "Prescriptions", "Appointments", "Other"
    };
}

public static class AmendmentStatuses
{
    public const string Pending = "Pendiente";
    public const string Accepted = "Aceptada";
    public const string Denied = "Denegada";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Pending, Accepted, Denied };
}
