using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Medication;
using Infrastructure.Core.Interfaces.Medication;
using Infrastructure.Core.Models.Medication;

namespace Infrastructure.Core.Services.Medication;

/// <summary>
/// Handles all database operations for the Medication catalog.
/// </summary>
public sealed class MedicationRepository : BaseDatabaseService, IMedicationRepository
{
    private readonly IDbConnection _connection;

    public MedicationRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<MedicationRow>, MedicationRepositoryError>> GetPageAsync(
        int offset, int limit, string? search) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, MedicationRow>(
                _connection,
                MedicationRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search }),
            errorFactory: MedicationRepositoryError (ex) => new GetMedicationsPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<MedicationRow?, MedicationRepositoryError>> InsertAsync(
        int pharmaceuticalFormId, int administrationRouteId,
        string genericName, string? commercialName, string concentration) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, MedicationRow>(
                _connection,
                MedicationRepositorySql.Insert,
                new
                {
                    PharmaceuticalFormId = pharmaceuticalFormId,
                    AdministrationRouteId = administrationRouteId,
                    GenericName = genericName,
                    CommercialName = commercialName,
                    Concentration = concentration
                }),
            errorFactory: MedicationRepositoryError (ex) => new InsertMedicationError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<MedicationRow?, MedicationRepositoryError>> UpdateAsync(
        Guid code, int pharmaceuticalFormId, int administrationRouteId,
        string genericName, string? commercialName, string concentration) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, MedicationRow>(
                _connection,
                MedicationRepositorySql.Update,
                new
                {
                    Code = code,
                    PharmaceuticalFormId = pharmaceuticalFormId,
                    AdministrationRouteId = administrationRouteId,
                    GenericName = genericName,
                    CommercialName = commercialName,
                    Concentration = concentration
                }),
            errorFactory: MedicationRepositoryError (ex) => new UpdateMedicationError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, MedicationRepositoryError>> DeactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                MedicationRepositorySql.Deactivate,
                new { Code = code, UserCode = performedByUserCode }),
            errorFactory: MedicationRepositoryError (ex) => new DeactivateMedicationError(ex.Message, ex)
        );
}
