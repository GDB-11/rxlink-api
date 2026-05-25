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
        string names, string surnames, DateOnly birthDate, string phone,
        string? alternativePhone, string email, string? address,
        string? emergencyContactName, string? emergencyContactPhone,
        string medicalRecordNumber) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PatientRow>(
                _connection,
                PatientRepositorySql.Insert,
                new
                {
                    Names = names,
                    Surnames = surnames,
                    BirthDate = birthDate,
                    Phone = phone,
                    AlternativePhone = alternativePhone,
                    Email = email,
                    Address = address,
                    EmergencyContactName = emergencyContactName,
                    EmergencyContactPhone = emergencyContactPhone,
                    MedicalRecordNumber = medicalRecordNumber
                }),
            errorFactory: PatientRepositoryError (ex) => new InsertPatientError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PatientRow?, PatientRepositoryError>> UpdateAsync(
        Guid code, string names, string surnames, DateOnly birthDate, string phone,
        string? alternativePhone, string email, string? address,
        string? emergencyContactName, string? emergencyContactPhone,
        string medicalRecordNumber) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PatientRow>(
                _connection,
                PatientRepositorySql.Update,
                new
                {
                    Code = code,
                    Names = names,
                    Surnames = surnames,
                    BirthDate = birthDate,
                    Phone = phone,
                    AlternativePhone = alternativePhone,
                    Email = email,
                    Address = address,
                    EmergencyContactName = emergencyContactName,
                    EmergencyContactPhone = emergencyContactPhone,
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
}
