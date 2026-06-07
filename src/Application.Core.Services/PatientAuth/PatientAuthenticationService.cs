using Application.Core.Config;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.PatientAuth.Errors;
using Application.Core.DTOs.PatientAuth.Request;
using Application.Core.DTOs.PatientAuth.Response;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.PatientAuth;
using Application.Core.Interfaces.Shared;
using BindSharp;
using BindSharp.Extensions;
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

    public PatientAuthenticationService(
        IPatientCredentialRepository repository,
        IPassword passwordService,
        IJwt jwtService,
        IDeterministicEncryption deterministicEncryption,
        ITimeProvider timeProvider,
        JwtConfig jwtConfig)
    {
        _repository = repository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _deterministicEncryption = deterministicEncryption;
        _timeProvider = timeProvider;
        _jwtConfig = jwtConfig;
    }

    public async Task<Result<PatientLookupResponse, PatientAuthError>> LookupAsync(
        Guid documentTypeCode,
        string documentNumber)
    {
        var result = await _repository.GetByDocumentAsync(documentTypeCode, documentNumber);

        return result
            .MapError(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception))
            .Map(row => row is null
                ? new PatientLookupResponse { Found = false }
                : new PatientLookupResponse
                {
                    Found = true,
                    PersonCode = row.PersonCode,
                    Names = row.Names,
                    Surnames = row.Surnames,
                    Email = row.Email,
                    IsAlreadyPatient = row.PatientCode.HasValue
                });
    }

    public async Task<Result<PatientAuthResponse, PatientAuthError>> RegisterAsync(RegisterPatientRequest request)
    {
        Result<string, PatientAuthError> hashResult = _passwordService
            .HashPassword(request.Password)
            .MapError(PatientAuthError (e) => new PatientPasswordHashError(e.Message, null, e.Exception));

        if (!hashResult.IsSuccess)
            return Result<PatientAuthResponse, PatientAuthError>.Failure(hashResult.Error!);

        string passwordHash = hashResult.Value!;

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
                        .AddCredentialsAsync(row.PersonCode, passwordHash)
                        .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception))
                    : _repository
                        .CreatePatientForPersonAsync(row.PersonCode, passwordHash)
                        .MapErrorAsync(PatientAuthError (e) =>
                            new PatientRepositoryError(e.Message, null, e.Exception)));
        }
        else
        {
            NewPatientData data = new()
            {
                Names = request.Names,
                Surnames = request.Surnames,
                BirthDate = request.BirthDate,
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
                .CreatePersonAndPatientAsync(data, passwordHash)
                .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception));
        }

        return await credentialResult.BindAsync(GenerateAndStoreTokens);
    }

    public Task<Result<PatientAuthResponse, PatientAuthError>> LoginAsync(PatientLoginRequest request) =>
        _repository.GetByEmailAsync(request.Email)
            .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .EnsureAsync(p => p.IsActive, new PatientInactiveError())
            .EnsureAsync(p => p.PasswordHash is not null, new PatientNoCredentialsError())
            .BindAsync(patient => ValidatePasswordAndGenerateTokens(patient, request.Password));

    public Task<Result<PatientAuthResponse, PatientAuthError>> RefreshAsync(PatientRefreshRequest request) =>
        EncryptTokenForLookup(request.RefreshToken)
            .BindAsync(hash => _repository
                .GetByRefreshTokenAsync(hash, _timeProvider.UtcNow)
                .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception)))
            .EnsureNotNullAsync(new PatientRefreshTokenNotFoundError())
            .EnsureAsync(p => p.IsActive, new PatientInactiveError())
            .BindAsync(GenerateAndStoreTokens);

    public Task<Result<Unit, PatientAuthError>> LogoutAsync(Guid patientCode) =>
        _repository.ClearRefreshTokenAsync(patientCode)
            .MapErrorAsync(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception));

    private Task<Result<PatientAuthResponse, PatientAuthError>> ValidatePasswordAndGenerateTokens(
        PatientCredential patient,
        string password) =>
        _passwordService
            .VerifyPassword(password, patient.PasswordHash!)
            .MapError(PatientAuthError (e) => new PatientPasswordHashError(e.Message, null, e.Exception))
            .Ensure(isValid => isValid, new PatientIncorrectPasswordError())
            .BindAsync(_ => GenerateAndStoreTokens(patient));

    private Task<Result<PatientAuthResponse, PatientAuthError>> GenerateAndStoreTokens(PatientCredential patient)
    {
        string rawToken = _jwtService.GenerateRefreshToken();

        return _jwtService
            .GeneratePatientAccessToken(patient)
            .MapError(PatientAuthError (e) => new PatientJwtGenerationError(e.Details, e.Exception))
            .BindAsync(tokens =>
                _deterministicEncryption.Encrypt(rawToken)
                    .MapError(PatientAuthError (e) => new PatientJwtStorageError(e.Details, e.Exception))
                    .BindAsync(tokenHash =>
                        _repository
                            .UpdateRefreshTokenAsync(
                                patient.PatientCode,
                                tokenHash,
                                _timeProvider.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes))
                            .MapErrorAsync(PatientAuthError (e) =>
                                new PatientRepositoryError(e.Message, null, e.Exception))
                            .MapAsync(_ =>
                                BuildAuthResponse(patient, tokens.AccessToken, rawToken, tokens.ExpiresAt))));
    }

    private Task<Result<string, PatientAuthError>> EncryptTokenForLookup(string rawToken) =>
        Task.FromResult(
            _deterministicEncryption.Encrypt(rawToken)
                .MapError(PatientAuthError (e) => new PatientRepositoryError(e.Message, null, e.Exception)));

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