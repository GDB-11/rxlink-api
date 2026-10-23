using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.PatientAuth;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.PatientAuth;
using Infrastructure.Core.Models.PatientAuth;

namespace Infrastructure.Core.Services.PatientAuth;

public sealed class PatientCredentialRepository : BaseDatabaseService, IPatientCredentialRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public PatientCredentialRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    public async Task<Result<PatientRegistrationCheckRow?, PatientCredentialError>> GetByDocumentAsync(
        Guid documentTypeCode,
        string documentNumber,
        DateTime birthDate) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, PatientRegistrationCheckRow?>(
                _connection,
                PatientCredentialRepositorySql.GetByDocument,
                new { DocumentTypeCode = documentTypeCode, DocumentNumber = documentNumber, BirthDate = birthDate }),
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

    public async Task<Result<PatientCredential, PatientCredentialError>> CreatePersonAndPatientAsync(
        NewPatientData data,
        string passwordHash) =>
        await Result.TryAsync(
            operation: async () =>
            {
                PatientCredential? row = await ExecuteAuditedSingleOrDefaultAsync<object, PatientCredential?>(_connection, _userContext,
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
                PatientCredential? row = await ExecuteAuditedSingleOrDefaultAsync<object, PatientCredential?>(_connection, _userContext,
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
                PatientCredential? row = await ExecuteAuditedSingleOrDefaultAsync<object, PatientCredential?>(_connection, _userContext,
                    PatientCredentialRepositorySql.AddCredentials,
                    new { PersonCode = personCode, PasswordHash = passwordHash });
                return row!;
            },
            errorFactory: PatientCredentialError (ex) => new AddCredentialsAsyncError(ex.Message, ex)
        );

    public async Task<Result<Unit, PatientCredentialError>> StoreRefreshTokenAsync(
        Guid patientCode,
        Guid familyCode,
        string tokenHash,
        DateTime expiresAt,
        DateTime now) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PatientCredentialRepositorySql.StoreRefreshToken,
                new
                {
                    PatientCode = patientCode,
                    FamilyCode = familyCode,
                    TokenHash = tokenHash,
                    ExpiresAt = expiresAt,
                    Now = now
                }),
            errorFactory: PatientCredentialError (ex) => new StoreRefreshTokenAsyncError(ex.Message, ex)
        ).MapAsync(_ => Unit.Value);

    public async Task<Result<PatientCredential?, PatientCredentialError>> RotateRefreshTokenAsync(
        string tokenHash,
        string newTokenHash,
        DateTime newExpiresAt,
        DateTime now) =>
        await Result.TryAsync(
            operation: async () => await ExecuteSingleOrDefaultAsync<object, PatientCredential?>(
                _connection,
                PatientCredentialRepositorySql.RotateRefreshToken,
                new { TokenHash = tokenHash, NewTokenHash = newTokenHash, NewExpiresAt = newExpiresAt, Now = now }),
            errorFactory: PatientCredentialError (ex) => new RotateRefreshTokenAsyncError(ex.Message, ex)
        );

    public async Task<Result<int, PatientCredentialError>> RevokeFamilyOfUsedTokenAsync(
        string tokenHash,
        DateTime now) =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, int?>(
                _connection,
                PatientCredentialRepositorySql.RevokeFamilyOfUsedToken,
                new { TokenHash = tokenHash, Now = now }).ContinueWith(t => t.Result ?? 0),
            errorFactory: PatientCredentialError (ex) => new RevokeSessionsAsyncError(ex.Message, ex)
        );

    public async Task<Result<Unit, PatientCredentialError>> RevokeAllSessionsAsync(Guid patientCode, DateTime now) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PatientCredentialRepositorySql.RevokeAllSessions,
                new { PatientCode = patientCode, Now = now }),
            errorFactory: PatientCredentialError (ex) => new RevokeSessionsAsyncError(ex.Message, ex)
        ).MapAsync(_ => Unit.Value);
}
