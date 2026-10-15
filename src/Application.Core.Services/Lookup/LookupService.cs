using Application.Core.DTOs.Inventory.Response;
using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.Lookup;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Lookup;
using Infrastructure.Core.Models.PartnerCatalog;

namespace Application.Core.Services.Lookup;

/// <summary>
/// Selector lists. Shared-catalog items come from the in-memory snapshot (active ones, and for medications,
/// specialties and insurers only those this partner has enabled); partner-owned lists still come from the database.
/// </summary>
public sealed class LookupService : ILookup
{
    private readonly ILookupRepository _repository;
    private readonly ICatalogSnapshotStore _store;
    private readonly IPartnerCatalogRepository _partner;

    public LookupService(ILookupRepository repository, ICatalogSnapshotStore store, IPartnerCatalogRepository partner)
    {
        _repository = repository;
        _store = store;
        _partner = partner;
    }

    /// <inheritdoc/>
    public async Task<Result<UserLookupsResponse, LookupError>> GetUserLookupsAsync()
    {
        if (_store.Current is not { } catalog) return Unavailable<UserLookupsResponse>();

        var roles = await _repository.GetActiveRolesAsync();
        if (!roles.IsSuccess) return Failure<UserLookupsResponse>(roles.Error.Message, roles.Error.Details, roles.Error.Exception);

        var specialties = await _partner.GetAllSpecialtiesAsync();
        if (!specialties.IsSuccess) return Failure<UserLookupsResponse>(specialties.Error);
        HashSet<Guid> enabled = EnabledSpecialties(specialties.Value);

        return Result<UserLookupsResponse, LookupError>.Success(new UserLookupsResponse
        {
            Sexes = [.. catalog.Sexes.Values.Where(s => s.IsActive).OrderBy(s => s.Name).Select(s => Item(s.Code, s.Name))],
            DocumentTypes = [.. catalog.DocumentTypes.Values.Where(d => d.IsActive).OrderBy(d => d.Name).Select(d => Item(d.Code, d.Name))],
            Roles = [.. roles.Value.Select(ToGuidItem)],
            Specialties = [.. catalog.Specialties.Values.Where(s => s.IsActive && enabled.Contains(s.Code)).OrderBy(s => s.Name).Select(s => Item(s.Code, s.Name))]
        });
    }

    /// <inheritdoc/>
    public Task<Result<PatientLookupsResponse, LookupError>> GetPatientLookupsAsync() =>
        Task.FromResult(_store.Current is not { } catalog
            ? Unavailable<PatientLookupsResponse>()
            : Result<PatientLookupsResponse, LookupError>.Success(new PatientLookupsResponse
            {
                AllergySeverities =
                [
                    .. catalog.AllergySeverities.Values.Where(s => s.IsActive).OrderBy(s => s.SortOrder)
                        .Select(s => Item(s.Code, s.Name))
                ]
            }));

    /// <inheritdoc/>
    public async Task<Result<PrescriptionLookupsResponse, LookupError>> GetPrescriptionLookupsAsync()
    {
        if (_store.Current is not { } catalog) return Unavailable<PrescriptionLookupsResponse>();

        var statuses = await _repository.GetActivePrescriptionStatusesAsync();
        if (!statuses.IsSuccess) return Failure<PrescriptionLookupsResponse>(statuses.Error.Message, statuses.Error.Details, statuses.Error.Exception);

        var settings = await _partner.GetAllMedicationsAsync();
        if (!settings.IsSuccess) return Failure<PrescriptionLookupsResponse>(settings.Error);
        Dictionary<Guid, PartnerMedicationRow> enabled = settings.Value.Where(m => m.IsEnabled).ToDictionary(m => m.MedicationCode);

        return Result<PrescriptionLookupsResponse, LookupError>.Success(new PrescriptionLookupsResponse
        {
            PrescriptionStatuses = [.. statuses.Value.Select(ToGuidItem)],
            Medications =
            [
                .. catalog.Medications.Values
                    .Where(m => m.IsActive && m.DeletedAt is null && enabled.ContainsKey(m.Code))
                    .OrderBy(m => m.GenericName, StringComparer.OrdinalIgnoreCase)
                    .Select(m => new MedicationLookupItemResponse
                    {
                        Code = m.Code,
                        Name = m.CommercialName is null ? m.GenericName : $"{m.CommercialName} - {m.GenericName}",
                        DefaultDose = m.Concentration,
                        DefaultAdministrationRouteCode = m.AdministrationRouteCode,
                        StockStatus = StockStatus.From(enabled[m.Code].StockQuantity, enabled[m.Code].ReorderLevel)
                    })
            ],
            AdministrationRoutes =
            [
                .. catalog.AdministrationRoutes.Values.Where(r => r.IsActive).OrderBy(r => r.Name)
                    .Select(r => Item(r.Code, r.Name))
            ],
            Frequencies =
            [
                .. catalog.Frequencies.Values.Where(f => f.IsActive).OrderBy(f => f.IntervalHours)
                    .Select(f => Item(f.Code, f.Description))
            ]
        });
    }

    /// <inheritdoc/>
    public async Task<Result<AppointmentLookupsResponse, LookupError>> GetAppointmentLookupsAsync()
    {
        if (_store.Current is not { } catalog) return Unavailable<AppointmentLookupsResponse>();

        var types = await _repository.GetActiveConsultationTypesAsync();
        if (!types.IsSuccess) return Failure<AppointmentLookupsResponse>(types.Error.Message, types.Error.Details, types.Error.Exception);

        var specialtySettings = await _partner.GetAllSpecialtiesAsync();
        if (!specialtySettings.IsSuccess) return Failure<AppointmentLookupsResponse>(specialtySettings.Error);

        var insuranceSettings = await _partner.GetAllInsurancesAsync();
        if (!insuranceSettings.IsSuccess) return Failure<AppointmentLookupsResponse>(insuranceSettings.Error);
        var insurances = insuranceSettings.Value.Where(i => i.IsEnabled).ToDictionary(i => i.InsuranceCode);
        var specialties = specialtySettings.Value.Where(s => s.IsEnabled).ToDictionary(s => s.SpecialtyCode);

        return Result<AppointmentLookupsResponse, LookupError>.Success(new AppointmentLookupsResponse
        {
            ConsultationTypes = [.. types.Value.Select(ToGuidItem)],
            Insurances =
            [
                .. catalog.Insurances.Values
                    .Where(i => i.IsActive && insurances.ContainsKey(i.Code))
                    .OrderBy(i => i.Name)
                    .Select(i => new InsuranceLookupItemResponse
                    {
                        Code = i.Code,
                        Name = i.Name,
                        CoveragePercentage = insurances[i.Code].CoveragePercentageOverride ?? i.DefaultCoveragePercentage
                    })
            ],
            Specialties =
            [
                .. catalog.Specialties.Values
                    .Where(s => s.IsActive && specialties.ContainsKey(s.Code))
                    .OrderBy(s => s.Name)
                    .Select(s => new SpecialtyPricingLookupItemResponse
                    {
                        Code = s.Code,
                        PriceInPerson = specialties[s.Code].PriceInPerson,
                        PriceVirtual = specialties[s.Code].PriceVirtual
                    })
            ]
        });
    }

    private static HashSet<Guid> EnabledSpecialties(IReadOnlyList<Infrastructure.Core.Models.PartnerCatalog.PartnerSpecialtyRow> rows) =>
        rows.Where(s => s.IsEnabled).Select(s => s.SpecialtyCode).ToHashSet();

    private static GuidLookupItemResponse Item(Guid code, string name) => new() { Code = code, Name = name };

    private static GuidLookupItemResponse ToGuidItem(GuidLookupRow row) =>
        new() { Code = row.Code, Name = row.Name };

    private static Result<T, LookupError> Unavailable<T>() =>
        Result<T, LookupError>.Failure(new LookupCatalogUnavailableError());

    private static Result<T, LookupError> Failure<T>(string message, string? details, Exception? exception) =>
        Result<T, LookupError>.Failure(new LookupDataAccessError(message, details, exception));

    private static Result<T, LookupError> Failure<T>(PartnerCatalogRepositoryError error) =>
        Failure<T>(error.Message, error.Details, error.Exception);
}
