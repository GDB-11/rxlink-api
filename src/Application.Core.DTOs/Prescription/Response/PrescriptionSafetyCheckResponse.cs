namespace Application.Core.DTOs.Prescription.Response;

public sealed class PrescriptionSafetyCheckResponse
{
    /// <summary>Allergy warnings first, then interactions from most to least severe. Empty when nothing was found.</summary>
    public required IReadOnlyList<PrescriptionSafetyWarning> Warnings { get; init; }
}

public static class SafetyWarningKinds
{
    public const string Allergy = "Allergy";
    public const string Interaction = "Interaction";
}

public static class InteractionSeverities
{
    public const string Minor = "Minor";
    public const string Moderate = "Moderate";
    public const string Major = "Major";
    public const string Contraindicated = "Contraindicated";

    /// <summary>Higher is more severe; unknown values rank lowest.</summary>
    public static int Rank(string severity) => severity switch
    {
        Contraindicated => 4,
        Major => 3,
        Moderate => 2,
        Minor => 1,
        _ => 0
    };
}

public sealed class PrescriptionSafetyWarning
{
    /// <see cref="SafetyWarningKinds"/>
    public required string Kind { get; init; }

    /// <summary>Allergy: the patient's recorded severity (may be null). Interaction: <see cref="InteractionSeverities"/>.</summary>
    public string? Severity { get; init; }

    /// <summary>The catalog allergy or interaction rule that produced the warning.</summary>
    public required Guid RuleCode { get; init; }

    public required string Message { get; init; }

    /// <summary>Medications involved (generic names), in the same order as <see cref="MedicationCodes"/>.</summary>
    public required IReadOnlyList<Guid> MedicationCodes { get; init; }
    public required IReadOnlyList<string> MedicationNames { get; init; }

    /// <summary>Set when the other medication of an interaction is on a different, already active prescription.</summary>
    public Guid? OtherPrescriptionCode { get; init; }
}
