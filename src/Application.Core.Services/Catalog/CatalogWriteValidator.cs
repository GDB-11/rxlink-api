using Application.Core.DTOs.Catalog.Errors;
using Application.Core.Interfaces.Catalog;
using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Application.Core.Services.Catalog;

public sealed class CatalogWriteValidator : ICatalogWriteValidator
{
    private static readonly Result<Unit, CatalogValidationError> Ok = Result<Unit, CatalogValidationError>.Success(Unit.Value);

    private readonly ICatalogSnapshotStore _store;
    private readonly IPartnerEnablementRepository _enablement;

    public CatalogWriteValidator(ICatalogSnapshotStore store, IPartnerEnablementRepository enablement)
    {
        _store = store;
        _enablement = enablement;
    }

    /// <inheritdoc/>
    public async Task<Result<Unit, CatalogValidationError>> ValidatePrescriptionLinesAsync(
        IReadOnlyCollection<(Guid MedicationCode, Guid AdministrationRouteCode, Guid FrequencyCode)> lines)
    {
        Result<CatalogSnapshot, CatalogValidationError> snapshot = Snapshot();
        if (!snapshot.IsSuccess) return Fail(snapshot);
        CatalogSnapshot catalog = snapshot.Value;

        foreach ((Guid medication, Guid route, Guid frequency) in lines)
        {
            Result<Unit, CatalogValidationError> check =
                Check(catalog.Medications, medication, CatalogItemKind.Medication, m => m.IsActive && m.DeletedAt is null);
            if (!check.IsSuccess) return check;
            check = Check(catalog.AdministrationRoutes, route, CatalogItemKind.AdministrationRoute, r => r.IsActive);
            if (!check.IsSuccess) return check;
            check = Check(catalog.Frequencies, frequency, CatalogItemKind.Frequency, f => f.IsActive);
            if (!check.IsSuccess) return check;
        }

        return await RequireEnabledAsync(PartnerEnablementKind.Medication, CatalogItemKind.Medication,
            lines.Select(l => l.MedicationCode));
    }

    /// <inheritdoc/>
    public async Task<Result<Unit, CatalogValidationError>> ValidateSpecialtyAsync(Guid? specialtyCode)
    {
        if (specialtyCode is not { } code) return Ok;

        Result<CatalogSnapshot, CatalogValidationError> snapshot = Snapshot();
        if (!snapshot.IsSuccess) return Fail(snapshot);

        Result<Unit, CatalogValidationError> check =
            Check(snapshot.Value.Specialties, code, CatalogItemKind.Specialty, s => s.IsActive);
        return check.IsSuccess
            ? await RequireEnabledAsync(PartnerEnablementKind.Specialty, CatalogItemKind.Specialty, [code])
            : check;
    }

    /// <inheritdoc/>
    public async Task<Result<Unit, CatalogValidationError>> ValidateInsuranceAsync(Guid? insuranceCode)
    {
        if (insuranceCode is not { } code) return Ok;

        Result<CatalogSnapshot, CatalogValidationError> snapshot = Snapshot();
        if (!snapshot.IsSuccess) return Fail(snapshot);

        Result<Unit, CatalogValidationError> check =
            Check(snapshot.Value.Insurances, code, CatalogItemKind.Insurance, i => i.IsActive);
        return check.IsSuccess
            ? await RequireEnabledAsync(PartnerEnablementKind.Insurance, CatalogItemKind.Insurance, [code])
            : check;
    }

    /// <inheritdoc/>
    public Task<Result<Unit, CatalogValidationError>> ValidatePersonAsync(Guid sexCode, Guid documentTypeCode)
    {
        Result<CatalogSnapshot, CatalogValidationError> snapshot = Snapshot();
        if (!snapshot.IsSuccess) return Task.FromResult(Fail(snapshot));

        Result<Unit, CatalogValidationError> check =
            Check(snapshot.Value.Sexes, sexCode, CatalogItemKind.Sex, s => s.IsActive);
        return Task.FromResult(check.IsSuccess
            ? Check(snapshot.Value.DocumentTypes, documentTypeCode, CatalogItemKind.DocumentType, d => d.IsActive)
            : check);
    }

    /// <inheritdoc/>
    public Task<Result<Unit, CatalogValidationError>> ValidateAllergiesAsync(
        IReadOnlyCollection<(Guid AllergyCode, Guid SeverityCode)> allergies)
    {
        Result<CatalogSnapshot, CatalogValidationError> snapshot = Snapshot();
        if (!snapshot.IsSuccess) return Task.FromResult(Fail(snapshot));

        foreach ((Guid allergy, Guid severity) in allergies)
        {
            Result<Unit, CatalogValidationError> check =
                Check(snapshot.Value.Allergies, allergy, CatalogItemKind.Allergy, a => a.IsActive);
            if (!check.IsSuccess) return Task.FromResult(check);
            check = Check(snapshot.Value.AllergySeverities, severity, CatalogItemKind.AllergySeverity, s => s.IsActive);
            if (!check.IsSuccess) return Task.FromResult(check);
        }

        return Task.FromResult(Ok);
    }

    private Result<CatalogSnapshot, CatalogValidationError> Snapshot() =>
        _store.Current is { } current
            ? Result<CatalogSnapshot, CatalogValidationError>.Success(current)
            : Result<CatalogSnapshot, CatalogValidationError>.Failure(new CatalogValidationUnavailableError());

    private static Result<Unit, CatalogValidationError> Fail(Result<CatalogSnapshot, CatalogValidationError> snapshot) =>
        Result<Unit, CatalogValidationError>.Failure(snapshot.Error);

    private static Result<Unit, CatalogValidationError> Check<TRow>(
        IReadOnlyDictionary<Guid, TRow> rows, Guid code, CatalogItemKind kind, Func<TRow, bool> isActive)
    {
        if (!rows.TryGetValue(code, out TRow? row))
            return Result<Unit, CatalogValidationError>.Failure(new CatalogItemNotFoundError(kind));

        return isActive(row)
            ? Ok
            : Result<Unit, CatalogValidationError>.Failure(new CatalogItemInactiveError(kind));
    }

    private async Task<Result<Unit, CatalogValidationError>> RequireEnabledAsync(
        PartnerEnablementKind enablementKind, CatalogItemKind kind, IEnumerable<Guid> codes)
    {
        Guid[] distinct = codes.Distinct().ToArray();
        Result<IReadOnlySet<Guid>, CatalogRepositoryError> enabled =
            await _enablement.GetEnabledAsync(enablementKind, distinct);

        if (!enabled.IsSuccess)
            return Result<Unit, CatalogValidationError>.Failure(
                new CatalogValidationDataAccessError(enabled.Error.Message, enabled.Error.Details, enabled.Error.Exception));

        return distinct.All(enabled.Value.Contains)
            ? Ok
            : Result<Unit, CatalogValidationError>.Failure(new CatalogItemNotEnabledError(kind));
    }
}
