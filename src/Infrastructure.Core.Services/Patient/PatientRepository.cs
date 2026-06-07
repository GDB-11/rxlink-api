using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Patient;
using Infrastructure.Core.Interfaces.Patient;
using Infrastructure.Core.Models.Patient;


namespace Infrastructure.Core.Services.Patient;

/// <summary>
/// Handles all database operations for the Patient entity.
/// </summary>
public sealed class PatientRepository : BaseDatabaseService, IPatientRepository
{
    private readonly IDbConnection _connection;

    public PatientRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<PatientRow>, PatientRepositoryError>> GetPageAsync(
        int offset, int limit, string? search) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, PatientRow>(
                _connection,
                PatientRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search }),
            errorFactory: PatientRepositoryError (ex) => new GetPatientsPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientRow?, PatientRepositoryError>> InsertAsync(
        Guid personCode, string allergiesJson) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PatientRow>(
                _connection,
                PatientRepositorySql.Insert,
                new
                {
                    PersonCode = personCode,
                    AllergiesJson = allergiesJson
                }),
            errorFactory: PatientRepositoryError (ex) => new InsertPatientError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientRow?, PatientRepositoryError>> UpdateAsync(
        Guid code, string medicalRecordNumber) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PatientRow>(
                _connection,
                PatientRepositorySql.Update,
                new
                {
                    Code = code,
                    MedicalRecordNumber = medicalRecordNumber
                }),
            errorFactory: PatientRepositoryError (ex) => new UpdatePatientError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PatientRepositoryError>> DeactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PatientRepositorySql.Deactivate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: PatientRepositoryError (ex) => new DeactivatePatientError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PatientRepositoryError>> ActivateAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PatientRepositorySql.Activate,
                new { Code = code }),
            errorFactory: PatientRepositoryError (ex) => new DeactivatePatientError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientAllergyRow?, PatientRepositoryError>> AddAllergyAsync(
        Guid patientCode, Guid allergyCode, Guid severityCode, string? notes) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PatientAllergyRow>(
                _connection,
                PatientRepositorySql.AddAllergy,
                new { PatientCode = patientCode, AllergyCode = allergyCode, SeverityCode = severityCode, Notes = notes }),
            errorFactory: PatientRepositoryError (ex) => new AddPatientAllergyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientAllergyRow?, PatientRepositoryError>> UpdateAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, Guid severityCode, string? notes) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PatientAllergyRow>(
                _connection,
                PatientRepositorySql.UpdateAllergy,
                new { PatientCode = patientCode, PatientAllergyCode = patientAllergyCode, SeverityCode = severityCode, Notes = notes }),
            errorFactory: PatientRepositoryError (ex) => new UpdatePatientAllergyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PatientRepositoryError>> DeleteAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PatientRepositorySql.DeleteAllergy,
                new { PatientCode = patientCode, PatientAllergyCode = patientAllergyCode, PerformedByUserCode = performedByUserCode }),
            errorFactory: PatientRepositoryError (ex) => new DeletePatientAllergyError(ex.Message, ex)
        );
}
