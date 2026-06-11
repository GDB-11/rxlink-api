using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.PatientAuth;
using Infrastructure.Core.Interfaces.PatientAuth;
using Infrastructure.Core.Models.PatientAuth;

namespace Infrastructure.Core.Services.PatientAuth;

public sealed class PatientCredentialRepository : BaseDatabaseService, IPatientCredentialRepository
{
    private readonly IDbConnection _connection;

    public PatientCredentialRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Result<PatientRegistrationCheckRow?, PatientCredentialError>> GetByDocumentAsync(
        Guid documentTypeCode,
        string documentNumber) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, PatientRegistrationCheckRow?>(
                _connection,
                PatientCredentialRepositorySql.GetByDocument,
                new { DocumentTypeCode = documentTypeCode, DocumentNumber = documentNumber }),
            errorFactory: PatientCredentialError (ex) => new GetByDocumentAsyncError(ex.Message, ex)
        );

    public async Task<Result<PatientRegistrationCheckRow?, PatientCredentialError>> GetByPersonCodeAsync(
        Guid personCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, PatientRegistrationCheckRow?>(
                _connection,
                PatientCredentialRepositorySql.GetByPersonCode,
                new { PersonCode = personCode }),
            errorFactory: PatientCredentialError (ex) => new GetByPersonCodeAsyncError(ex.Message, ex)
        );

    public async Task<Result<PatientCredential?, PatientCredentialError>> GetByEmailAsync(string email) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, PatientCredential?>(
                _connection,
                PatientCredentialRepositorySql.GetByEmail,
                new { Email = email }),
            errorFactory: PatientCredentialError (ex) => new GetByEmailAsyncError(ex.Message, ex)
        );

    public async Task<Result<PatientCredential?, PatientCredentialError>> GetByRefreshTokenAsync(
        string tokenHash,
        DateTime currentDate) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, PatientCredential?>(
                _connection,
                PatientCredentialRepositorySql.GetByRefreshToken,
                new { TokenHash = tokenHash, CurrentDate = currentDate }),
            errorFactory: PatientCredentialError (ex) => new GetByRefreshTokenAsyncError(ex.Message, ex)
        );

    public async Task<Result<PatientCredential, PatientCredentialError>> CreatePersonAndPatientAsync(
        NewPatientData data,
        string passwordHash) =>
        await Result.TryAsync(
            operation: async () =>
            {
                PatientCredential? row = await ExecuteSingleOrDefaultAsync<object, PatientCredential?>(
                    _connection,
                    PatientCredentialRepositorySql.CreatePersonAndPatient,
                    new
                    {
                        data.Names,
                        data.Surnames,
                        data.BirthDate,
                        data.SexCode,
                        data.Phone,
                        data.AlternativePhone,
                        data.Email,
                        data.Address,
                        data.EmergencyContactName,
                        data.EmergencyContactPhone,
                        data.DocumentTypeCode,
                        data.DocumentNumber,
                        PasswordHash = passwordHash
                    });
                return row!;
            },
            errorFactory: PatientCredentialError (ex) => new CreatePersonAndPatientAsyncError(ex.Message, ex)
        );

    public async Task<Result<PatientCredential, PatientCredentialError>> CreatePatientForPersonAsync(
        Guid personCode,
        string passwordHash) =>
        await Result.TryAsync(
            operation: async () =>
            {
                PatientCredential? row = await ExecuteSingleOrDefaultAsync<object, PatientCredential?>(
                    _connection,
                    PatientCredentialRepositorySql.CreatePatientForPerson,
                    new { PersonCode = personCode, PasswordHash = passwordHash });
                return row!;
            },
            errorFactory: PatientCredentialError (ex) => new CreatePatientForPersonAsyncError(ex.Message, ex)
        );

    public async Task<Result<PatientCredential, PatientCredentialError>> AddCredentialsAsync(
        Guid personCode,
        string passwordHash) =>
        await Result.TryAsync(
            operation: async () =>
            {
                PatientCredential? row = await ExecuteSingleOrDefaultAsync<object, PatientCredential?>(
                    _connection,
                    PatientCredentialRepositorySql.AddCredentials,
                    new { PersonCode = personCode, PasswordHash = passwordHash });
                return row!;
            },
            errorFactory: PatientCredentialError (ex) => new AddCredentialsAsyncError(ex.Message, ex)
        );

    public async Task<Result<Unit, PatientCredentialError>> UpdateRefreshTokenAsync(
        Guid patientCode,
        string tokenHash,
        DateTime expiresAt) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PatientCredentialRepositorySql.UpdateRefreshToken,
                new { PatientCode = patientCode, TokenHash = tokenHash, ExpiresAt = expiresAt }),
            errorFactory: PatientCredentialError (ex) => new UpdateRefreshTokenAsyncError(ex.Message, ex)
        ).MapAsync(_ => Unit.Value);

    public async Task<Result<Unit, PatientCredentialError>> ClearRefreshTokenAsync(Guid patientCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PatientCredentialRepositorySql.ClearRefreshToken,
                new { PatientCode = patientCode }),
            errorFactory: PatientCredentialError (ex) => new ClearRefreshTokenAsyncError(ex.Message, ex)
        ).MapAsync(_ => Unit.Value);
}