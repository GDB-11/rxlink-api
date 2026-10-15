using Application.Core.DTOs.Prescription.Response;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Application.Core.Services.Prescription;

/// <summary>A medication the patient already takes through another active prescription.</summary>
public sealed record OtherActiveMedication(Guid PrescriptionCode, Guid MedicationCode);

/// <summary>An allergy recorded for the patient.</summary>
public sealed record RecordedAllergy(Guid AllergyCode, string AllergyName, string? SeverityName);

/// <summary>
/// Pure rule engine: given a catalog snapshot, the medications of a prescription, the patient's allergies and the
/// medications of the patient's other active prescriptions, returns the warnings. No I/O.
/// </summary>
public static class PrescriptionSafetyEvaluator
{
    public static IReadOnlyList<PrescriptionSafetyWarning> Evaluate(
        CatalogSnapshot snapshot,
        IReadOnlyCollection<Guid> prescriptionMedications,
        IReadOnlyCollection<RecordedAllergy> allergies,
        IReadOnlyCollection<OtherActiveMedication> otherActive)
    {
        List<Guid> current = prescriptionMedications.Distinct().ToList();

        List<PrescriptionSafetyWarning> warnings = [];
        warnings.AddRange(AllergyWarnings(snapshot, current, allergies));
        warnings.AddRange(InteractionWarnings(snapshot, current, otherActive));

        return warnings
            .OrderBy(w => w.Kind == SafetyWarningKinds.Allergy ? 0 : 1)
            .ThenByDescending(w => w.Kind == SafetyWarningKinds.Interaction
                ? InteractionSeverities.Rank(w.Severity ?? "")
                : 0)
            .ToList();
    }

    private static IEnumerable<PrescriptionSafetyWarning> AllergyWarnings(
        CatalogSnapshot snapshot, List<Guid> medications, IReadOnlyCollection<RecordedAllergy> allergies)
    {
        foreach (RecordedAllergy allergy in allergies)
        {
            if (!snapshot.ClassesByAllergy.TryGetValue(allergy.AllergyCode, out IReadOnlyList<Guid>? allergyClasses))
                continue;

            foreach (Guid medication in medications)
            {
                Guid? shared = ClassesOf(snapshot, medication).Cast<Guid?>()
                    .FirstOrDefault(c => allergyClasses.Contains(c!.Value));
                if (shared is null)
                    continue;

                string medicationName = NameOf(snapshot, medication);
                string className = snapshot.MedicationClasses[shared.Value].Name;
                yield return new PrescriptionSafetyWarning
                {
                    Kind = SafetyWarningKinds.Allergy,
                    Severity = allergy.SeverityName,
                    RuleCode = allergy.AllergyCode,
                    Message = $"El paciente tiene alergia a «{allergy.AllergyName}» y {medicationName} " +
                              $"pertenece a la clase «{className}».",
                    MedicationCodes = [medication],
                    MedicationNames = [medicationName]
                };
            }
        }
    }

    private static IEnumerable<PrescriptionSafetyWarning> InteractionWarnings(
        CatalogSnapshot snapshot, List<Guid> current, IReadOnlyCollection<OtherActiveMedication> otherActive)
    {
        if (snapshot.MedicationInteractions.Count == 0)
            yield break;

        var seen = new HashSet<(Guid Rule, Guid X, Guid Y)>();

        // Pairs inside this prescription
        for (int i = 0; i < current.Count; i++)
        for (int j = i + 1; j < current.Count; j++)
            foreach (PrescriptionSafetyWarning w in PairWarnings(snapshot, current[i], current[j], null, seen))
                yield return w;

        // This prescription against the patient's other active ones
        foreach (Guid medication in current)
        foreach (OtherActiveMedication other in otherActive)
        {
            if (other.MedicationCode == medication)
                continue;
            foreach (PrescriptionSafetyWarning w in PairWarnings(snapshot, medication, other.MedicationCode,
                         other.PrescriptionCode, seen))
                yield return w;
        }
    }

    private static IEnumerable<PrescriptionSafetyWarning> PairWarnings(
        CatalogSnapshot snapshot, Guid x, Guid y, Guid? otherPrescription,
        HashSet<(Guid Rule, Guid X, Guid Y)> seen)
    {
        IReadOnlyList<Guid> classesX = ClassesOf(snapshot, x), classesY = ClassesOf(snapshot, y);

        foreach (MedicationInteractionCatalogRow rule in snapshot.MedicationInteractions)
        {
            bool matches =
                (classesX.Contains(rule.ClassACode) && classesY.Contains(rule.ClassBCode)) ||
                (classesX.Contains(rule.ClassBCode) && classesY.Contains(rule.ClassACode));
            if (!matches)
                continue;

            (Guid, Guid, Guid) key = x.CompareTo(y) <= 0 ? (rule.Code, x, y) : (rule.Code, y, x);
            if (!seen.Add(key))
                continue;

            string nameX = NameOf(snapshot, x), nameY = NameOf(snapshot, y);
            yield return new PrescriptionSafetyWarning
            {
                Kind = SafetyWarningKinds.Interaction,
                Severity = rule.Severity,
                RuleCode = rule.Code,
                Message = $"{nameX} + {nameY}: {rule.Description}",
                MedicationCodes = [x, y],
                MedicationNames = [nameX, nameY],
                OtherPrescriptionCode = otherPrescription
            };
        }
    }

    private static IReadOnlyList<Guid> ClassesOf(CatalogSnapshot snapshot, Guid medication) =>
        snapshot.ClassesByMedication.TryGetValue(medication, out IReadOnlyList<Guid>? classes) ? classes : [];

    private static string NameOf(CatalogSnapshot snapshot, Guid medication) =>
        snapshot.Medications.TryGetValue(medication, out MedicationCatalogRow? row)
            ? row.GenericName
            : medication.ToString();
}
