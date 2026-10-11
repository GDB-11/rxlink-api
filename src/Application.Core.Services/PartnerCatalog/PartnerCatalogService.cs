using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.PartnerCatalog.Errors;
using Application.Core.DTOs.PartnerCatalog.Request;
using Application.Core.DTOs.PartnerCatalog.Response;
using Application.Core.Interfaces.PartnerCatalog;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.PartnerCatalog;

namespace Application.Core.Services.PartnerCatalog;

public sealed class PartnerCatalogService : IPartnerCatalog
{
    private readonly ICatalogSnapshotStore _store;
    private readonly IPartnerCatalogRepository _repository;

    public PartnerCatalogService(ICatalogSnapshotStore store, IPartnerCatalogRepository repository)
    {
        _store = store;
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<PartnerMedicationResponse, PartnerCatalogError>> UpdateMedicationAsync(
        Guid code, UpdatePartnerMedicationRequest request)
    {
        PartnerCatalogError? rejected = Reject(CatalogItemKind.Medication, request.IsEnabled,
            snapshot => snapshot.Medications.TryGetValue(code, out var m) ? m.IsActive && m.DeletedAt is null : null);
        if (rejected is not null) return Failure<PartnerMedicationResponse>(rejected);

        var current = await _repository.GetMedicationAsync(code);
        if (!current.IsSuccess) return Failure<PartnerMedicationResponse>(DataAccess(current.Error));

        var saved = await _repository.SaveMedicationAsync(
            code, request.IsEnabled ?? current.Value.IsEnabled, request.ReorderLevel);
        return saved.IsSuccess
            ? Result<PartnerMedicationResponse, PartnerCatalogError>.Success(new PartnerMedicationResponse
            {
                MedicationCode = saved.Value.MedicationCode,
                IsEnabled = saved.Value.IsEnabled,
                ReorderLevel = saved.Value.ReorderLevel
            })
            : Failure<PartnerMedicationResponse>(DataAccess(saved.Error));
    }

    /// <inheritdoc/>
    public async Task<Result<PartnerSpecialtyResponse, PartnerCatalogError>> UpdateSpecialtyAsync(
        Guid code, UpdatePartnerSpecialtyRequest request)
    {
        PartnerCatalogError? rejected = Reject(CatalogItemKind.Specialty, request.IsEnabled,
            snapshot => snapshot.Specialties.TryGetValue(code, out var s) ? s.IsActive : null);
        if (rejected is not null) return Failure<PartnerSpecialtyResponse>(rejected);

        var current = await _repository.GetSpecialtyAsync(code);
        if (!current.IsSuccess) return Failure<PartnerSpecialtyResponse>(DataAccess(current.Error));

        PartnerSpecialtyRow row = current.Value;
        bool isEnabled = request.IsEnabled ?? row.IsEnabled;
        decimal inPerson = request.PriceInPerson ?? row.PriceInPerson;
        decimal virtualPrice = request.PriceVirtual ?? row.PriceVirtual;

        // Same rule the table enforces (V017): an offered specialty must have real prices.
        if (isEnabled && (inPerson <= 0 || virtualPrice <= 0))
            return Failure<PartnerSpecialtyResponse>(new PartnerCatalogRuleError(
                "Una especialidad habilitada debe tener precio presencial y virtual mayores a cero."));

        var saved = await _repository.SaveSpecialtyAsync(code, isEnabled, inPerson, virtualPrice);
        return saved.IsSuccess
            ? Result<PartnerSpecialtyResponse, PartnerCatalogError>.Success(new PartnerSpecialtyResponse
            {
                SpecialtyCode = saved.Value.SpecialtyCode,
                IsEnabled = saved.Value.IsEnabled,
                PriceInPerson = saved.Value.PriceInPerson,
                PriceVirtual = saved.Value.PriceVirtual
            })
            : Failure<PartnerSpecialtyResponse>(DataAccess(saved.Error));
    }

    /// <inheritdoc/>
    public async Task<Result<PartnerInsuranceResponse, PartnerCatalogError>> UpdateInsuranceAsync(
        Guid code, UpdatePartnerInsuranceRequest request)
    {
        if (request is { ClearCoverageOverride: true, CoveragePercentageOverride: not null })
            return Failure<PartnerInsuranceResponse>(new PartnerCatalogRuleError(
                "No se puede enviar un porcentaje de cobertura y solicitar quitarlo a la vez."));

        PartnerCatalogError? rejected = Reject(CatalogItemKind.Insurance, request.IsEnabled,
            snapshot => snapshot.Insurances.TryGetValue(code, out var i) ? i.IsActive : null);
        if (rejected is not null) return Failure<PartnerInsuranceResponse>(rejected);

        var current = await _repository.GetInsuranceAsync(code);
        if (!current.IsSuccess) return Failure<PartnerInsuranceResponse>(DataAccess(current.Error));

        decimal? coverage = request.ClearCoverageOverride
            ? null
            : request.CoveragePercentageOverride ?? current.Value.CoveragePercentageOverride;

        var saved = await _repository.SaveInsuranceAsync(code, request.IsEnabled ?? current.Value.IsEnabled, coverage);
        return saved.IsSuccess
            ? Result<PartnerInsuranceResponse, PartnerCatalogError>.Success(new PartnerInsuranceResponse
            {
                InsuranceCode = saved.Value.InsuranceCode,
                IsEnabled = saved.Value.IsEnabled,
                CoveragePercentageOverride = saved.Value.CoveragePercentageOverride
            })
            : Failure<PartnerInsuranceResponse>(DataAccess(saved.Error));
    }

    /// <summary>
    /// Checks the code against the snapshot. <paramref name="isActive"/> returns <c>null</c> when the code is unknown.
    /// A deactivated item may still be disabled, but not enabled.
    /// </summary>
    private PartnerCatalogError? Reject(CatalogItemKind kind, bool? enabling, Func<Infrastructure.Core.Interfaces.Catalog.CatalogSnapshot, bool?> isActive)
    {
        if (_store.Current is not { } snapshot)
            return new PartnerCatalogItemRejectedError(new CatalogValidationUnavailableError());

        return isActive(snapshot) switch
        {
            null => new PartnerCatalogItemRejectedError(new CatalogItemNotFoundError(kind)),
            false when enabling == true => new PartnerCatalogItemRejectedError(new CatalogItemInactiveError(kind)),
            _ => null
        };
    }

    private static PartnerCatalogError DataAccess(PartnerCatalogRepositoryError error) =>
        new PartnerCatalogDataAccessError(error.Message, error.Details, error.Exception);

    private static Result<T, PartnerCatalogError> Failure<T>(PartnerCatalogError error) =>
        Result<T, PartnerCatalogError>.Failure(error);
}
