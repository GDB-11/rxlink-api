namespace Application.Core.DTOs.PatientAccess;

/// <summary>Outcome of <c>IPatientAccessPolicy.EvaluateAsync</c>.</summary>
public enum PatientAccessDecision
{
    Allowed,

    /// <summary>The caller is outside the patient's care team but holds an unexpired break-glass grant.</summary>
    AllowedViaBreakGlass,

    Denied
}

/// <summary>What kind of patient data the caller wants to read.</summary>
public enum PatientDataScope
{
    /// <summary>Identity and contact data, appointments and payments.</summary>
    Administrative,

    /// <summary>Allergies, diagnostics and prescription detail.</summary>
    Clinical
}

/// <summary>How a patient list must be narrowed for the caller.</summary>
/// <param name="RestrictToCareTeamOfUserCode">When set, only patients in the care team (or with a live break-glass grant) for this user are listed.</param>
/// <param name="IncludeClinical">False when clinical fields (allergies) must be blanked in the response.</param>
public sealed record PatientListScope(Guid? RestrictToCareTeamOfUserCode, bool IncludeClinical);
