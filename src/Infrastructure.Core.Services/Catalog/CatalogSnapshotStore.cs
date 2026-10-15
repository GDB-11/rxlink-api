using System.Collections.Frozen;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Infrastructure.Core.Services.Catalog;

public sealed class CatalogSnapshotStore : ICatalogSnapshotStore
{
    private readonly ICatalogVersionReader _versionReader;
    private readonly ICatalogReader<DocumentTypeCatalogRow> _documentTypes;
    private readonly ICatalogReader<SexCatalogRow> _sexes;
    private readonly ICatalogReader<SpecialtyCatalogRow> _specialties;
    private readonly ICatalogReader<PharmaceuticalFormCatalogRow> _pharmaceuticalForms;
    private readonly ICatalogReader<AdministrationRouteCatalogRow> _administrationRoutes;
    private readonly ICatalogReader<FrequencyCatalogRow> _frequencies;
    private readonly ICatalogReader<AllergyCatalogRow> _allergies;
    private readonly ICatalogReader<AllergySeverityCatalogRow> _allergySeverities;
    private readonly ICatalogReader<MedicationCatalogRow> _medications;
    private readonly ICatalogReader<InsuranceCatalogRow> _insurances;
    private readonly ICatalogReader<MedicationClassCatalogRow> _medicationClasses;
    private readonly ICatalogReader<MedicationClassMemberCatalogRow> _medicationClassMembers;
    private readonly ICatalogReader<AllergyMedicationClassCatalogRow> _allergyMedicationClasses;
    private readonly ICatalogReader<MedicationInteractionCatalogRow> _medicationInteractions;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private CatalogSnapshot? _current;
    private long _lastFailureTicks;

    public CatalogSnapshotStore(
        ICatalogVersionReader versionReader,
        ICatalogReader<DocumentTypeCatalogRow> documentTypes,
        ICatalogReader<SexCatalogRow> sexes,
        ICatalogReader<SpecialtyCatalogRow> specialties,
        ICatalogReader<PharmaceuticalFormCatalogRow> pharmaceuticalForms,
        ICatalogReader<AdministrationRouteCatalogRow> administrationRoutes,
        ICatalogReader<FrequencyCatalogRow> frequencies,
        ICatalogReader<AllergyCatalogRow> allergies,
        ICatalogReader<AllergySeverityCatalogRow> allergySeverities,
        ICatalogReader<MedicationCatalogRow> medications,
        ICatalogReader<InsuranceCatalogRow> insurances,
        ICatalogReader<MedicationClassCatalogRow> medicationClasses,
        ICatalogReader<MedicationClassMemberCatalogRow> medicationClassMembers,
        ICatalogReader<AllergyMedicationClassCatalogRow> allergyMedicationClasses,
        ICatalogReader<MedicationInteractionCatalogRow> medicationInteractions,
        TimeProvider clock)
    {
        _versionReader = versionReader;
        _documentTypes = documentTypes;
        _sexes = sexes;
        _specialties = specialties;
        _pharmaceuticalForms = pharmaceuticalForms;
        _administrationRoutes = administrationRoutes;
        _frequencies = frequencies;
        _allergies = allergies;
        _allergySeverities = allergySeverities;
        _medications = medications;
        _insurances = insurances;
        _medicationClasses = medicationClasses;
        _medicationClassMembers = medicationClassMembers;
        _allergyMedicationClasses = allergyMedicationClasses;
        _medicationInteractions = medicationInteractions;
        _clock = clock;
    }

    public CatalogSnapshot? Current => Volatile.Read(ref _current);

    public DateTimeOffset? LastFailureAt
    {
        get
        {
            long ticks = Interlocked.Read(ref _lastFailureTicks);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    public async Task<Result<CatalogSnapshot, CatalogRepositoryError>> RefreshIfChangedAsync()
    {
        CatalogSnapshot? current = Current;
        if (current is null)
            return await RefreshAsync();

        Result<CatalogVersionRow, CatalogRepositoryError> version = await _versionReader.GetAsync();
        if (version.IsFailure)
        {
            RecordFailure();
            return Result<CatalogSnapshot, CatalogRepositoryError>.Failure(version.Error);
        }

        if (version.Value.Version == current.Version && version.Value.SchemaVersion == current.SchemaVersion)
        {
            Interlocked.Exchange(ref _lastFailureTicks, 0);
            return Result<CatalogSnapshot, CatalogRepositoryError>.Success(current);
        }

        return await RefreshAsync();
    }

    public async Task<Result<CatalogSnapshot, CatalogRepositoryError>> RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            // The version is read BEFORE the tables: if a write lands in between, the snapshot is stamped with the
            // older version, so the next poll sees a newer one and reloads. Never the reverse (stale data
            // stamped as current).
            Result<CatalogSnapshot, CatalogRepositoryError> result = await _versionReader.GetAsync()
                .BindAsync(BuildAsync);

            if (result.IsSuccess)
            {
                Volatile.Write(ref _current, result.Value);
                Interlocked.Exchange(ref _lastFailureTicks, 0);
            }
            else
            {
                RecordFailure();
            }

            return result;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void RecordFailure() =>
        Interlocked.Exchange(ref _lastFailureTicks, _clock.GetUtcNow().UtcTicks);

    private async Task<Result<CatalogSnapshot, CatalogRepositoryError>> BuildAsync(CatalogVersionRow version)
    {
        // The readers are independent (each opens its own pooled connection), so run them together.
        var documentTypes = _documentTypes.GetAllAsync();
        var sexes = _sexes.GetAllAsync();
        var specialties = _specialties.GetAllAsync();
        var pharmaceuticalForms = _pharmaceuticalForms.GetAllAsync();
        var administrationRoutes = _administrationRoutes.GetAllAsync();
        var frequencies = _frequencies.GetAllAsync();
        var allergies = _allergies.GetAllAsync();
        var allergySeverities = _allergySeverities.GetAllAsync();
        var medications = _medications.GetAllAsync();
        var insurances = _insurances.GetAllAsync();
        var medicationClasses = _medicationClasses.GetAllAsync();
        var classMembers = _medicationClassMembers.GetAllAsync();
        var allergyClasses = _allergyMedicationClasses.GetAllAsync();
        var interactions = _medicationInteractions.GetAllAsync();

        await Task.WhenAll(documentTypes, sexes, specialties, pharmaceuticalForms, administrationRoutes,
            frequencies, allergies, allergySeverities, medications, insurances,
            medicationClasses, classMembers, allergyClasses, interactions);

        CatalogRepositoryError? failure = new[]
        {
            Failure(documentTypes.Result), Failure(sexes.Result), Failure(specialties.Result),
            Failure(pharmaceuticalForms.Result), Failure(administrationRoutes.Result),
            Failure(frequencies.Result), Failure(allergies.Result), Failure(allergySeverities.Result),
            Failure(medications.Result), Failure(insurances.Result),
            Failure(medicationClasses.Result), Failure(classMembers.Result),
            Failure(allergyClasses.Result), Failure(interactions.Result)
        }.FirstOrDefault(error => error is not null);

        if (failure is not null)
            return Result<CatalogSnapshot, CatalogRepositoryError>.Failure(failure);

        return Result<CatalogSnapshot, CatalogRepositoryError>.Success(new CatalogSnapshot
        {
            Version = version.Version,
            SchemaVersion = version.SchemaVersion,
            LoadedAt = _clock.GetUtcNow(),
            DocumentTypes = Index(documentTypes.Result.Value, r => r.Code),
            Sexes = Index(sexes.Result.Value, r => r.Code),
            Specialties = Index(specialties.Result.Value, r => r.Code),
            PharmaceuticalForms = Index(pharmaceuticalForms.Result.Value, r => r.Code),
            AdministrationRoutes = Index(administrationRoutes.Result.Value, r => r.Code),
            Frequencies = Index(frequencies.Result.Value, r => r.Code),
            Allergies = Index(allergies.Result.Value, r => r.Code),
            AllergySeverities = Index(allergySeverities.Result.Value, r => r.Code),
            Medications = Index(medications.Result.Value, r => r.Code),
            Insurances = Index(insurances.Result.Value, r => r.Code),
            MedicationClasses = Index(medicationClasses.Result.Value, r => r.Code),
            ClassesByMedication = Group(ActiveMembers(classMembers.Result.Value, medicationClasses.Result.Value),
                r => r.MedicationCode, r => r.ClassCode),
            ClassesByAllergy = Group(allergyClasses.Result.Value
                    .Where(r => IsActiveClass(medicationClasses.Result.Value, r.ClassCode)),
                r => r.AllergyCode, r => r.ClassCode),
            MedicationInteractions = interactions.Result.Value
                .Where(r => IsActiveClass(medicationClasses.Result.Value, r.ClassACode)
                            && IsActiveClass(medicationClasses.Result.Value, r.ClassBCode))
                .ToList()
        });
    }

    private static CatalogRepositoryError? Failure<T>(Result<IReadOnlyList<T>, CatalogRepositoryError> result) =>
        result.IsFailure ? result.Error : null;

    private static bool IsActiveClass(IReadOnlyList<MedicationClassCatalogRow> classes, Guid code) =>
        classes.Any(c => c.Code == code && c.IsActive);

    private static IEnumerable<MedicationClassMemberCatalogRow> ActiveMembers(
        IReadOnlyList<MedicationClassMemberCatalogRow> members, IReadOnlyList<MedicationClassCatalogRow> classes)
    {
        HashSet<Guid> active = classes.Where(c => c.IsActive).Select(c => c.Code).ToHashSet();
        return members.Where(m => active.Contains(m.ClassCode));
    }

    private static FrozenDictionary<Guid, IReadOnlyList<Guid>> Group<T>(
        IEnumerable<T> rows, Func<T, Guid> key, Func<T, Guid> value) =>
        rows.GroupBy(key)
            .ToFrozenDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(value).Distinct().ToList());

    private static FrozenDictionary<Guid, T> Index<T>(IReadOnlyList<T> rows, Func<T, Guid> code) =>
        rows.ToFrozenDictionary(code);
}
