using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Patient;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Patient;
using Infrastructure.Core.Models.Patient;


namespace Infrastructure.Core.Services.Patient;

/// <summary>
/// Handles all database operations for the Patient entity.
/// </summary>
public sealed class PatientRepository : BaseDatabaseService, IPatientRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public PatientRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<PatientRow>, PatientRepositoryError>> GetPageAsync(
        int offset, int limit, string? search, Guid? careTeamOfUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, PatientRow>(
                _connection,
                PatientRepositorySql.GetPage,
                new { Offset = offset, Limit = limit, Search = search, UserCode = careTeamOfUserCode }),
            errorFactory: PatientRepositoryError (ex) => new GetPatientsPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientRow?, PatientRepositoryError>> InsertAsync(
        Guid personCode, string allergiesJson) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, PatientRow>(_connection, _userContext,
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
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, PatientRow>(_connection, _userContext,
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
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                PatientRepositorySql.Deactivate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: PatientRepositoryError (ex) => new DeactivatePatientError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PatientRepositoryError>> ActivateAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                PatientRepositorySql.Activate,
                new { Code = code }),
            errorFactory: PatientRepositoryError (ex) => new DeactivatePatientError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientAllergyRow?, PatientRepositoryError>> AddAllergyAsync(
        Guid patientCode, Guid allergyCode, Guid severityCode, string? notes) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, PatientAllergyRow>(_connection, _userContext,
                PatientRepositorySql.AddAllergy,
                new
                {
                    PatientCode = patientCode, AllergyCode = allergyCode, SeverityCode = severityCode, Notes = notes
                }),
            errorFactory: PatientRepositoryError (ex) => new AddPatientAllergyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientAllergyRow?, PatientRepositoryError>> UpdateAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, Guid severityCode, string? notes) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, PatientAllergyRow>(_connection, _userContext,
                PatientRepositorySql.UpdateAllergy,
                new
                {
                    PatientCode = patientCode, PatientAllergyCode = patientAllergyCode, SeverityCode = severityCode,
                    Notes = notes
                }),
            errorFactory: PatientRepositoryError (ex) => new UpdatePatientAllergyError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientRow?, PatientRepositoryError>> GetByCodeAsync(Guid patientCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PatientRow>(
                _connection,
                PatientRepositorySql.GetByCode,
                new { PatientCode = patientCode }),
            errorFactory: PatientRepositoryError (ex) => new GetPatientByCodeError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientRow?, PatientRepositoryError>> GetByPersonCodeAsync(Guid personCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PatientRow>(
                _connection,
                PatientRepositorySql.GetByPersonCode,
                new { PersonCode = personCode }),
            errorFactory: PatientRepositoryError (ex) => new GetPatientByPersonCodeError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PatientRepositoryError>> UpdatePersonContactAsync(
        Guid patientCode, string phone, string? alternativePhone,
        string? address, string? emergencyContactName, string? emergencyContactPhone) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                PatientRepositorySql.UpdatePersonContact,
                new
                {
                    PatientCode = patientCode,
                    Phone = phone,
                    AlternativePhone = alternativePhone,
                    Address = address,
                    EmergencyContactName = emergencyContactName,
                    EmergencyContactPhone = emergencyContactPhone
                }),
            errorFactory: PatientRepositoryError (ex) => new UpdatePersonContactError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PatientRepositoryError>> DeleteAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                PatientRepositorySql.DeleteAllergy,
                new
                {
                    PatientCode = patientCode, PatientAllergyCode = patientAllergyCode,
                    PerformedByUserCode = performedByUserCode
                }),
            errorFactory: PatientRepositoryError (ex) => new DeletePatientAllergyError(ex.Message, ex)
        );
}