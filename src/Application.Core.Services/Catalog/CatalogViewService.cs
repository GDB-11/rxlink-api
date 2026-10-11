using Application.Core.DTOs.Allergy.Request;
using Application.Core.DTOs.Allergy.Response;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Catalog.Response;
using Application.Core.DTOs.Insurance.Request;
using Application.Core.DTOs.Insurance.Response;
using Application.Core.DTOs.Medication.Request;
using Application.Core.DTOs.Medication.Response;
using Application.Core.DTOs.Specialty.Request;
using Application.Core.DTOs.Specialty.Response;
using Application.Core.Interfaces.Catalog;
using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.PartnerCatalog;

namespace Application.Core.Services.Catalog;

public sealed class CatalogViewService : ICatalogView
{
    private readonly ICatalogSnapshotStore _store;
    private readonly IPartnerCatalogRepository _partner;

    public CatalogViewService(ICatalogSnapshotStore store, IPartnerCatalogRepository partner)
    {
        _store = store;
        _partner = partner;
    }

    /// <inheritdoc/>
    public async Task<Result<CatalogPageResponse<MedicationCatalogItemResponse>, CatalogReadError>> GetMedicationsPageAsync(
        MedicationPageRequest request)
    {
        if (_store.Current is not { } catalog) return Unavailable<MedicationCatalogItemResponse>();

        var settings = await _partner.GetAllMedicationsAsync();
        if (!settings.IsSuccess) return DataAccess<MedicationCatalogItemResponse>(settings.Error);
        Dictionary<Guid, PartnerMedicationRow> byCode = settings.Value.ToDictionary(r => r.MedicationCode);

        IEnumerable<MedicationCatalogItemResponse> items = catalog.Medications.Values
            .Select(m =>
            {
                byCode.TryGetValue(m.Code, out PartnerMedicationRow? setting);
                return ToResponse(m, setting, catalog);
            })
            .Where(m => !request.EnabledOnly || (m.IsActive && m.IsEnabled))
            .Where(m => Matches(request.Search, m.GenericName, m.CommercialName))
            .OrderBy(m => m.GenericName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.MedicationCode);

        return Page(items, request.Page, request.PageSize);
    }

    /// <inheritdoc/>
    public async Task<Result<CatalogPageResponse<SpecialtyCatalogItemResponse>, CatalogReadError>> GetSpecialtiesPageAsync(
        SpecialtyPageRequest request)
    {
        if (_store.Current is not { } catalog) return Unavailable<SpecialtyCatalogItemResponse>();

        var settings = await _partner.GetAllSpecialtiesAsync();
        if (!settings.IsSuccess) return DataAccess<SpecialtyCatalogItemResponse>(settings.Error);
        Dictionary<Guid, PartnerSpecialtyRow> byCode = settings.Value.ToDictionary(r => r.SpecialtyCode);

        IEnumerable<SpecialtyCatalogItemResponse> items = catalog.Specialties.Values
            .Select(s =>
            {
                byCode.TryGetValue(s.Code, out PartnerSpecialtyRow? setting);
                return new SpecialtyCatalogItemResponse
                {
                    SpecialtyCode = s.Code,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    IsEnabled = setting?.IsEnabled ?? false,
                    PriceInPerson = setting?.PriceInPerson ?? 0,
                    PriceVirtual = setting?.PriceVirtual ?? 0
                };
            })
            .Where(s => !request.EnabledOnly || (s.IsActive && s.IsEnabled))
            .Where(s => Matches(request.Search, s.Name))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.SpecialtyCode);

        return Page(items, request.Page, request.PageSize);
    }

    /// <inheritdoc/>
    public async Task<Result<CatalogPageResponse<InsuranceCatalogItemResponse>, CatalogReadError>> GetInsurancesPageAsync(
        InsurancePageRequest request)
    {
        if (_store.Current is not { } catalog) return Unavailable<InsuranceCatalogItemResponse>();

        var settings = await _partner.GetAllInsurancesAsync();
        if (!settings.IsSuccess) return DataAccess<InsuranceCatalogItemResponse>(settings.Error);
        Dictionary<Guid, PartnerInsuranceRow> byCode = settings.Value.ToDictionary(r => r.InsuranceCode);

        IEnumerable<InsuranceCatalogItemResponse> items = catalog.Insurances.Values
            .Select(i =>
            {
                byCode.TryGetValue(i.Code, out PartnerInsuranceRow? setting);
                decimal? coverageOverride = setting?.CoveragePercentageOverride;
                return new InsuranceCatalogItemResponse
                {
                    InsuranceCode = i.Code,
                    Name = i.Name,
                    IsActive = i.IsActive,
                    IsEnabled = setting?.IsEnabled ?? false,
                    DefaultCoveragePercentage = i.DefaultCoveragePercentage,
                    CoveragePercentageOverride = coverageOverride,
                    CoveragePercentage = coverageOverride ?? i.DefaultCoveragePercentage
                };
            })
            .Where(i => !request.EnabledOnly || (i.IsActive && i.IsEnabled))
            .Where(i => Matches(request.Search, i.Name))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.InsuranceCode);

        return Page(items, request.Page, request.PageSize);
    }

    /// <inheritdoc/>
    public Task<Result<CatalogPageResponse<AllergyResponse>, CatalogReadError>> GetAllergiesPageAsync(
        AllergyPageRequest request)
    {
        if (_store.Current is not { } catalog) return Task.FromResult(Unavailable<AllergyResponse>());

        IEnumerable<AllergyResponse> items = catalog.Allergies.Values
            .Where(a => Matches(request.Search, a.Name))
            .Select(a => new AllergyResponse
            {
                AllergyCode = a.Code,
                Name = a.Name,
                Description = a.Description,
                IsActive = a.IsActive
            })
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.AllergyCode);

        return Task.FromResult(Page(items, request.Page, request.PageSize));
    }

    private static MedicationCatalogItemResponse ToResponse(
        MedicationCatalogRow medication, PartnerMedicationRow? setting, CatalogSnapshot catalog) =>
        new()
        {
            MedicationCode = medication.Code,
            PharmaceuticalFormCode = medication.PharmaceuticalFormCode,
            PharmaceuticalFormName = catalog.PharmaceuticalForms.TryGetValue(medication.PharmaceuticalFormCode, out var form)
                ? form.Name
                : string.Empty,
            AdministrationRouteCode = medication.AdministrationRouteCode,
            AdministrationRouteName = catalog.AdministrationRoutes.TryGetValue(medication.AdministrationRouteCode, out var route)
                ? route.Name
                : string.Empty,
            GenericName = medication.GenericName,
            CommercialName = medication.CommercialName,
            Concentration = medication.Concentration,
            IsActive = medication.IsActive && medication.DeletedAt is null,
            IsEnabled = setting?.IsEnabled ?? false,
            ReorderLevel = setting?.ReorderLevel,
            StockQuantity = setting?.StockQuantity
        };

    private static bool Matches(string? search, params string?[] values) =>
        string.IsNullOrWhiteSpace(search) ||
        values.Any(v => v is not null && v.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));

    private static Result<CatalogPageResponse<T>, CatalogReadError> Page<T>(IEnumerable<T> ordered, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        List<T> all = ordered.ToList();
        return Result<CatalogPageResponse<T>, CatalogReadError>.Success(new CatalogPageResponse<T>
        {
            Items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = all.Count,
            Page = page,
            PageSize = pageSize,
            TotalPages = all.Count == 0 ? 0 : (int)Math.Ceiling(all.Count / (double)pageSize)
        });
    }

    private const int MaxPageSize = 200;

    private static Result<CatalogPageResponse<T>, CatalogReadError> Unavailable<T>() =>
        Result<CatalogPageResponse<T>, CatalogReadError>.Failure(new CatalogReadUnavailableError());

    private static Result<CatalogPageResponse<T>, CatalogReadError> DataAccess<T>(PartnerCatalogRepositoryError error) =>
        Result<CatalogPageResponse<T>, CatalogReadError>.Failure(
            new CatalogReadDataAccessError(error.Message, error.Details, error.Exception));
}
