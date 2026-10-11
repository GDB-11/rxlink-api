namespace Infrastructure.Core.Models.PatientAccess;

/// <summary>Database facts the patient access policy decides on for one (user, patient) pair.</summary>
public sealed class PatientAccessFactsRow
{
    /// <summary><c>Role.CanViewClinicalData</c> of the caller's role.</summary>
    public required bool CanViewClinicalData { get; init; }

    /// <summary>The patient has an <c>Activo</c> prescription or an appointment today.</summary>
    public required bool InCareTeam { get; init; }

    /// <summary>The caller holds an unexpired break-glass grant for the patient.</summary>
    public required bool HasBreakGlass { get; init; }
}
