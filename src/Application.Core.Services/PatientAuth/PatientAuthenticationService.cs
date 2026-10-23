using Application.Core.Interfaces.Catalog;
using Application.Core.Config;
using Application.Core.DTOs.PatientAuth.Errors;
using Application.Core.DTOs.PatientAuth.Request;
using Application.Core.DTOs.PatientAuth.Response;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.PatientAuth;
using Application.Core.Interfaces.Shared;
using BindSharp;
using BindSharp.Extensions;
using Common.Helpers;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.PatientAuth;
using Infrastructure.Core.Models.PatientAuth;

namespace Application.Core.Services.PatientAuth;

public sealed class PatientAuthenticationService : IPatientAuthentication
{
    private readonly IPatientCredentialRepository _repository;
    private readonly IPassword _passwordService;
    private readonly IJwt _jwtService;
    private readonly IDeterministicEncryption _deterministicEncryption;
    private readonly ITimeProvider _timeProvider;
    private readonly JwtConfig _jwtConfig;

    private readonly ICatalogWriteValidator _catalog;
    private readonly IActivityLogger _activityLogger;

    public PatientAuthenticationService(
        IPatientCredentialRepository repository,
        IPassword passwordService,
        IJwt jwtService,
        IDeterministicEncryption deterministicEncryption,
        ITimeProvider timeProvider,
        JwtConfig jwtConfig,
        ICatalogWriteValidator catalog,
        IActivityLogger activityLogger)
    {
        _activityLogger = activityLogger;
        _repository = repository;
        _catalog = catalog;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _deterministicEncryption = deterministicEncryption;
        _timeProvider = timeProvider;
        _jwtConfig = jwtConfig;
    }

    public async Task<Result<PatientLookupResponse, PatientAuthError>> LookupAsync(PatientLookupRequest request)
    {
        var result = await _repository.GetByDocumentAsync(
            request.DocumentTypeCode, request.DocumentNumber, request.BirthDate.ToDateTime());

        Result<PatientLookupResponse, PatientAuthError> response = result
            .MapError(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception))
            .Map(row => row is null
                ? new PatientLookupResponse { Found = false }
                : new PatientLookupResponse
                {
                    Found = true,
                    PersonCode = row.PersonCode,
                    HasAccount = row.HasCredentials
                });

        // No document number or birth date in the event: only that a lookup came back empty.
        if (response.IsSuccess && !response.Value.Found)
            await _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.PatientLookupFailed, null, null, null, "Patient lookup found no match"));

        return response;
    }

    public async Task<Result<PatientAuthResponse, PatientAuthError>> RegisterAsync(RegisterPatientRequest request)
    {
        Result<string, PatientAuthError> hashResult = _passwordService
            .HashPassword(request.Password)
            .MapError(PatientAuthError (e) => new PatientPasswordHashError(e.Message, null, e.Exception));

        if (!hashResult.IsSuccess)
            return Result<PatientAuthResponse, PatientAuthError>.Failure(hashResult.Error);

        if (!request.PersonCode.HasValue)
        {
            Result<Unit, PatientAuthError> catalogCheck = await _catalog
                .ValidatePersonAsync(request.SexCode, request.DocumentTypeCode)
                .MapErrorAsync(PatientAuthError (error) => new PatientAuthCatalogValidationError(error));
            if (!catalogCheck.IsSuccess)
                return Result<PatientAuthResponse, PatientAuthError>.Failure(catalogCheck.Error);
        }

        Result<PatientCredential, PatientAuthError> credentialResult;

        if (request.PersonCode.HasValue)
        {
            credentialResult = await _repository
                .GetByPersonCodeAsync(request.PersonCode.Value)
                .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception))
                .EnsureNotNullAsync(new PersonNotFoundError())
                .EnsureAsync(row => !row.HasCredentials, new PatientAlreadyRegisteredError())
                .BindAsync(row => row.PatientCode.HasValue
                    ? _repository
                        .AddCredentialsAsync(row.PersonCode, hashResult.Value)
                        .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception))
                    : _repository
                        .CreatePatientForPersonAsync(row.PersonCode, hashResult.Value)
                        .MapErrorAsync(PatientAuthError (e) =>
                            new PatientRepositoryError(e.Message, null, e.Exception)));
        }
        else
        {
            NewPatientData data = new()
            {
                Names = request.Names,
                Surnames = request.Surnames,
                BirthDate = request.BirthDate.ToDateTime(),
                SexCode = request.SexCode,
                Phone = request.Phone,
                AlternativePhone = request.AlternativePhone,
                Email = request.Email,
                Address = request.Address,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
                DocumentTypeCode = request.DocumentTypeCode,
                DocumentNumber = request.DocumentNumber
            };

            credentialResult = await _repository
                .CreatePersonAndPatientAsync(data, hashResult.Value)
                .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception));
        }

        return await credentialResult.BindAsync(GenerateAndStoreTokens);
    }

    // Unknown e-mail, no app account and wrong password all answer the same way (no account enumeration).
    public Task<Result<PatientAuthResponse, PatientAuthError>> LoginAsync(PatientLoginRequest request) =>
        _repository.GetByEmailAsync(request.Email)
            .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception))
            .EnsureNotNullAsync(new PatientIncorrectPasswordError())
            .EnsureAsync(p => p.PasswordHash is not null, new PatientIncorrectPasswordError())
            .BindAsync(patient => ValidatePasswordAndGenerateTokens(patient, request.Password))
            .TapAsync(response => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.PatientLoginSucceeded, null, ActivityEntityTypes.Patient,
                response.Patient.PatientCode, "Patient logged in")))
            .TapErrorAsync(error => error is PatientIncorrectPasswordError
                ? _activityLogger.LogAsync(new ActivityEvent(
                    ActivityEventTypes.PatientLoginFailed, null, null, null, "Patient login failed"))
                : Task.CompletedTask);

    /// <summary>
    /// Rotating refresh (D31): the presented token is single-use. Presenting one that was already used means it
    /// leaked, so the whole session family is revoked and the legitimate client has to log in again.
    /// </summary>
    public async Task<Result<PatientAuthResponse, PatientAuthError>> RefreshAsync(PatientRefreshRequest request)
    {
        Result<string, PatientAuthError> presented = EncryptToken(request.RefreshToken);
        if (!presented.IsSuccess)
            return Result<PatientAuthResponse, PatientAuthError>.Failure(presented.Error);

        DateTime now = _timeProvider.UtcNow;
        string newRaw = _jwtService.GenerateRefreshToken();
        Result<string, PatientAuthError> newHash = EncryptToken(newRaw);
        if (!newHash.IsSuccess)
            return Result<PatientAuthResponse, PatientAuthError>.Failure(newHash.Error);

        var rotated = await _repository.RotateRefreshTokenAsync(
            presented.Value, newHash.Value, now.AddMinutes(_jwtConfig.PatientRefreshTokenExpiryMinutes), now);
        if (!rotated.IsSuccess)
            return Result<PatientAuthResponse, PatientAuthError>.Failure(
                new PatientRepositoryError(rotated.Error.Message, null, rotated.Error.Exception));

        if (rotated.Value is null)
        {
            var revoked = await _repository.RevokeFamilyOfUsedTokenAsync(presented.Value, now);
            if (revoked.IsSuccess && revoked.Value > 0)
                await _activityLogger.LogAsync(new ActivityEvent(
                    ActivityEventTypes.RefreshTokenReuse, null, null, null,
                    "Patient refresh token reuse: session revoked", new { RevokedTokens = revoked.Value }));

            return Result<PatientAuthResponse, PatientAuthError>.Failure(new PatientRefreshTokenNotFoundError());
        }

        PatientCredential patient = rotated.Value;
        return _jwtService
            .GeneratePatientAccessToken(patient)
            .MapError(PatientAuthError (e) => new PatientJwtGenerationError(e.Details, e.Exception))
            .Map(tokens => BuildAuthResponse(patient, tokens.AccessToken, newRaw, tokens.ExpiresAt));
    }

    public async Task<Result<Unit, PatientAuthError>> LogoutAsync(Guid patientCode)
    {
        var revoked = await _repository.RevokeAllSessionsAsync(patientCode, _timeProvider.UtcNow);
        if (revoked.IsSuccess)
            await _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.Logout, null, ActivityEntityTypes.Patient, patientCode, "Patient logged out"));

        return revoked.MapError(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception));
    }

    private Task<Result<PatientAuthResponse, PatientAuthError>> ValidatePasswordAndGenerateTokens(
        PatientCredential patient,
        string password) =>
        _passwordService
            .VerifyPassword(password, patient.PasswordHash!)
            .MapError(PatientAuthError (e) => new PatientPasswordHashError(e.Message, null, e.Exception))
            .Ensure(isValid => isValid, new PatientIncorrectPasswordError())
            .BindAsync(_ => GenerateAndStoreTokens(patient));

    /// <summary>Starts a new session family (login or registration).</summary>
    private Task<Result<PatientAuthResponse, PatientAuthError>> GenerateAndStoreTokens(PatientCredential patient)
    {
        string rawToken = _jwtService.GenerateRefreshToken();
        DateTime now = _timeProvider.UtcNow;

        return _jwtService
            .GeneratePatientAccessToken(patient)
            .MapError(PatientAuthError (e) => new PatientJwtGenerationError(e.Details, e.Exception))
            .BindAsync(tokens =>
                _deterministicEncryption.Encrypt(rawToken)
                    .MapError(PatientAuthError (e) => new PatientJwtStorageError(e.Details, e.Exception))
                    .BindAsync(tokenHash =>
                        _repository
                            .StoreRefreshTokenAsync(
                                patient.PatientCode,
                                Guid.NewGuid(),
                                tokenHash,
                                now.AddMinutes(_jwtConfig.PatientRefreshTokenExpiryMinutes),
                                now)
                            .MapErrorAsync(PatientAuthError (e) =>
                                new PatientRepositoryError(e.Message, null, e.Exception))
                            .MapAsync(_ =>
                                BuildAuthResponse(patient, tokens.AccessToken, rawToken, tokens.ExpiresAt))));
    }

    private Result<string, PatientAuthError> EncryptToken(string rawToken) =>
        _deterministicEncryption.Encrypt(rawToken)
            .MapError(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception));

    private static PatientAuthResponse BuildAuthResponse(
        PatientCredential patient,
        string accessToken,
        string rawRefreshToken,
        DateTime expiresAt) =>
        new()
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt,
            Patient = new PatientInfo
            {
                PatientCode = patient.PatientCode,
                Names = patient.Names,
                Surnames = patient.Surnames,
                Email = patient.Email,
                MedicalRecordNumber = patient.MedicalRecordNumber
            }
        };
}