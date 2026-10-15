using System.Collections.Frozen;
using Application.Core.DTOs.Prescription.Response;
using Application.Core.Services.Prescription;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Application.Core.Services.Tests.Prescription;

public sealed class PrescriptionSafetyEvaluatorTests
{
    private static readonly Guid Amoxicillin = Guid.NewGuid(), Ampicillin = Guid.NewGuid(),
        Ibuprofen = Guid.NewGuid(), Warfarin = Guid.NewGuid(), Paracetamol = Guid.NewGuid();

    private static readonly Guid Penicillins = Guid.NewGuid(), Nsaids = Guid.NewGuid(), Anticoagulants = Guid.NewGuid();
    private static readonly Guid PenicillinAllergy = Guid.NewGuid();
    private static readonly Guid NsaidAnticoagulantRule = Guid.NewGuid(), NsaidNsaidRule = Guid.NewGuid();

    private static CatalogSnapshot Snapshot(params MedicationInteractionCatalogRow[] rules) => new()
    {
        Version = 1,
        SchemaVersion = 1,
        LoadedAt = DateTimeOffset.UtcNow,
        DocumentTypes = FrozenDictionary<Guid, DocumentTypeCatalogRow>.Empty,
        Sexes = FrozenDictionary<Guid, SexCatalogRow>.Empty,
        Specialties = FrozenDictionary<Guid, SpecialtyCatalogRow>.Empty,
        PharmaceuticalForms = FrozenDictionary<Guid, PharmaceuticalFormCatalogRow>.Empty,
        AdministrationRoutes = FrozenDictionary<Guid, AdministrationRouteCatalogRow>.Empty,
        Frequencies = FrozenDictionary<Guid, FrequencyCatalogRow>.Empty,
        Allergies = FrozenDictionary<Guid, AllergyCatalogRow>.Empty,
        AllergySeverities = FrozenDictionary<Guid, AllergySeverityCatalogRow>.Empty,
        Insurances = FrozenDictionary<Guid, InsuranceCatalogRow>.Empty,
        Medications = new[] { (Amoxicillin, "Amoxicilina"), (Ampicillin, "Ampicilina"), (Ibuprofen, "Ibuprofeno"),
                (Warfarin, "Warfarina"), (Paracetamol, "Paracetamol") }
            .ToFrozenDictionary(m => m.Item1, m => new MedicationCatalogRow
            {
                Code = m.Item1, PharmaceuticalFormCode = Guid.Empty, AdministrationRouteCode = Guid.Empty,
                GenericName = m.Item2, Concentration = "1", IsActive = true
            }),
        MedicationClasses = new[] { (Penicillins, "Penicilinas"), (Nsaids, "AINEs"), (Anticoagulants, "Anticoagulantes") }
            .ToFrozenDictionary(c => c.Item1, c => new MedicationClassCatalogRow
                { Code = c.Item1, Name = c.Item2, IsActive = true }),
        ClassesByMedication = new Dictionary<Guid, IReadOnlyList<Guid>>
        {
            [Amoxicillin] = [Penicillins], [Ampicillin] = [Penicillins],
            [Ibuprofen] = [Nsaids], [Warfarin] = [Anticoagulants]
        }.ToFrozenDictionary(),
        ClassesByAllergy = new Dictionary<Guid, IReadOnlyList<Guid>> { [PenicillinAllergy] = [Penicillins] }
            .ToFrozenDictionary(),
        MedicationInteractions = rules
    };

    private static MedicationInteractionCatalogRow Rule(Guid code, Guid a, Guid b, string severity) => new()
        { Code = code, ClassACode = a, ClassBCode = b, Severity = severity, Description = "riesgo" };

    private static readonly RecordedAllergy Allergy = new(PenicillinAllergy, "Penicilina", "Grave");

    [Fact]
    public void Allergy_WhenMedicationIsInAnImpliedClass_Warns()
    {
        var warnings = PrescriptionSafetyEvaluator.Evaluate(Snapshot(), [Amoxicillin, Paracetamol], [Allergy], []);

        PrescriptionSafetyWarning w = Assert.Single(warnings);
        Assert.Equal(SafetyWarningKinds.Allergy, w.Kind);
        Assert.Equal("Grave", w.Severity);
        Assert.Equal([Amoxicillin], w.MedicationCodes);
        Assert.Equal(PenicillinAllergy, w.RuleCode);
    }

    [Fact]
    public void Allergy_WithNoMatchingMedication_OrNoRecordedAllergy_DoesNotWarn()
    {
        Assert.Empty(PrescriptionSafetyEvaluator.Evaluate(Snapshot(), [Paracetamol, Ibuprofen], [Allergy], []));
        Assert.Empty(PrescriptionSafetyEvaluator.Evaluate(Snapshot(), [Amoxicillin], [], []));
    }

    [Fact]
    public void Interaction_BetweenTwoLinesOfTheSamePrescription_Warns_RegardlessOfRuleOrientation()
    {
        var rule = Rule(NsaidAnticoagulantRule, Anticoagulants, Nsaids, "Major");

        var warnings = PrescriptionSafetyEvaluator.Evaluate(Snapshot(rule), [Ibuprofen, Warfarin], [], []);

        PrescriptionSafetyWarning w = Assert.Single(warnings);
        Assert.Equal(SafetyWarningKinds.Interaction, w.Kind);
        Assert.Equal("Major", w.Severity);
        Assert.Null(w.OtherPrescriptionCode);
        Assert.Equal(2, w.MedicationCodes.Count);
    }

    [Fact]
    public void Interaction_WithAnotherActivePrescription_ReportsThatPrescription()
    {
        Guid other = Guid.NewGuid();
        var rule = Rule(NsaidAnticoagulantRule, Nsaids, Anticoagulants, "Contraindicated");

        var warnings = PrescriptionSafetyEvaluator.Evaluate(Snapshot(rule), [Ibuprofen], [],
            [new OtherActiveMedication(other, Warfarin)]);

        Assert.Equal(other, Assert.Single(warnings).OtherPrescriptionCode);
    }

    [Fact]
    public void Interaction_SameClassRule_NeedsTwoDifferentMedications()
    {
        var rule = Rule(NsaidNsaidRule, Nsaids, Nsaids, "Moderate");

        Assert.Empty(PrescriptionSafetyEvaluator.Evaluate(Snapshot(rule), [Ibuprofen], [],
            [new OtherActiveMedication(Guid.NewGuid(), Ibuprofen)]));
    }

    [Fact]
    public void Evaluate_ListsAllergiesFirst_ThenInteractionsBySeverity()
    {
        var minor = Rule(Guid.NewGuid(), Nsaids, Anticoagulants, "Minor");
        var major = Rule(Guid.NewGuid(), Penicillins, Anticoagulants, "Major");

        var warnings = PrescriptionSafetyEvaluator.Evaluate(Snapshot(minor, major),
            [Ibuprofen, Warfarin, Amoxicillin], [Allergy], []);

        Assert.Equal([SafetyWarningKinds.Allergy, SafetyWarningKinds.Interaction, SafetyWarningKinds.Interaction],
            warnings.Select(w => w.Kind));
        Assert.Equal(["Major", "Minor"], warnings.Skip(1).Select(w => w.Severity));
    }

    [Fact]
    public void Evaluate_WithoutRules_ReturnsNothing() =>
        Assert.Empty(PrescriptionSafetyEvaluator.Evaluate(Snapshot(), [Amoxicillin, Ibuprofen], [], []));
}
