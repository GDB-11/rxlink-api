using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.PartnerCatalog;

namespace Infrastructure.Core.Services.PartnerCatalog;

public sealed class PartnerCatalogRepository : BaseDatabaseService, IPartnerCatalogRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public PartnerCatalogRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public Task<Result<PartnerMedicationRow, PartnerCatalogRepositoryError>> GetMedicationAsync(Guid code) =>
        ReadAsync<PartnerMedicationRow>(PartnerCatalogRepositorySql.GetMedication, code);

    /// <inheritdoc/>
    public Task<Result<PartnerSpecialtyRow, PartnerCatalogRepositoryError>> GetSpecialtyAsync(Guid code) =>
        ReadAsync<PartnerSpecialtyRow>(PartnerCatalogRepositorySql.GetSpecialty, code);

    /// <inheritdoc/>
    public Task<Result<PartnerInsuranceRow, PartnerCatalogRepositoryError>> GetInsuranceAsync(Guid code) =>
        ReadAsync<PartnerInsuranceRow>(PartnerCatalogRepositorySql.GetInsurance, code);

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<PartnerMedicationRow>, PartnerCatalogRepositoryError>> GetAllMedicationsAsync() =>
        ReadAllAsync<PartnerMedicationRow>(PartnerCatalogRepositorySql.GetAllMedications);

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<PartnerSpecialtyRow>, PartnerCatalogRepositoryError>> GetAllSpecialtiesAsync() =>
        ReadAllAsync<PartnerSpecialtyRow>(PartnerCatalogRepositorySql.GetAllSpecialties);

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<PartnerInsuranceRow>, PartnerCatalogRepositoryError>> GetAllInsurancesAsync() =>
        ReadAllAsync<PartnerInsuranceRow>(PartnerCatalogRepositorySql.GetAllInsurances);

    /// <inheritdoc/>
    public async Task<Result<PartnerMedicationRow, PartnerCatalogRepositoryError>> SaveMedicationAsync(
        Guid code, bool isEnabled, decimal? reorderLevel) =>
        await Result.TryAsync(
            operation: async () =>
            {
                await ExecuteAuditedWriteAsync(_connection, _userContext, async transaction =>
                {
                    var args = new { Code = code, IsEnabled = isEnabled, ReorderLevel = reorderLevel, UserCode = _userContext.UserCode };
                    await ExecuteNonQueryAsync(_connection, PartnerCatalogRepositorySql.UpsertMedication, args, transaction);
                    if (reorderLevel is not null)
                        await ExecuteNonQueryAsync(_connection, PartnerCatalogRepositorySql.UpsertReorderLevel, args, transaction);
                    return 0;
                });
                return await ExecuteSingleOrDefaultAsync<object, PartnerMedicationRow>(
                    _connection, PartnerCatalogRepositorySql.GetMedication, new { Code = code })
                    ?? throw new InvalidOperationException("PartnerMedication row missing after save.");
            },
            errorFactory: PartnerCatalogRepositoryError (ex) => new SavePartnerCatalogSettingError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<PartnerSpecialtyRow, PartnerCatalogRepositoryError>> SaveSpecialtyAsync(
        Guid code, bool isEnabled, decimal priceInPerson, decimal priceVirtual) =>
        await Result.TryAsync(
            operation: async () =>
            {
                await ExecuteAuditedNonQueryAsync(_connection, _userContext, PartnerCatalogRepositorySql.UpsertSpecialty,
                    new { Code = code, IsEnabled = isEnabled, PriceInPerson = priceInPerson, PriceVirtual = priceVirtual, UserCode = _userContext.UserCode });
                return await ExecuteSingleOrDefaultAsync<object, PartnerSpecialtyRow>(
                    _connection, PartnerCatalogRepositorySql.GetSpecialty, new { Code = code })
                    ?? throw new InvalidOperationException("PartnerSpecialty row missing after save.");
            },
            errorFactory: PartnerCatalogRepositoryError (ex) => new SavePartnerCatalogSettingError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<PartnerInsuranceRow, PartnerCatalogRepositoryError>> SaveInsuranceAsync(
        Guid code, bool isEnabled, decimal? coveragePercentageOverride) =>
        await Result.TryAsync(
            operation: async () =>
            {
                await ExecuteAuditedNonQueryAsync(_connection, _userContext, PartnerCatalogRepositorySql.UpsertInsurance,
                    new { Code = code, IsEnabled = isEnabled, CoveragePercentageOverride = coveragePercentageOverride, UserCode = _userContext.UserCode });
                return await ExecuteSingleOrDefaultAsync<object, PartnerInsuranceRow>(
                    _connection, PartnerCatalogRepositorySql.GetInsurance, new { Code = code })
                    ?? throw new InvalidOperationException("PartnerInsurance row missing after save.");
            },
            errorFactory: PartnerCatalogRepositoryError (ex) => new SavePartnerCatalogSettingError(ex.Message, ex));

    private async Task<Result<IReadOnlyList<TRow>, PartnerCatalogRepositoryError>> ReadAllAsync<TRow>(string sql) where TRow : class =>
        await Result.TryAsync(
            operation: async () => (IReadOnlyList<TRow>)(await ExecuteQueryAsync<TRow>(_connection, sql)).ToList(),
            errorFactory: PartnerCatalogRepositoryError (ex) => new GetPartnerCatalogSettingError(ex.Message, ex));

    private async Task<Result<TRow, PartnerCatalogRepositoryError>> ReadAsync<TRow>(string sql, Guid code) where TRow : class =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, TRow>(_connection, sql, new { Code = code })
                                   ?? throw new InvalidOperationException("Setting query returned no row."),
            errorFactory: PartnerCatalogRepositoryError (ex) => new GetPartnerCatalogSettingError(ex.Message, ex));
}
