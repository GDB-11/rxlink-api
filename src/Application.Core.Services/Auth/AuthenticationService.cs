using Application.Core.Config;
using Application.Core.DTOs.Account;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Auth.Request;
using Application.Core.DTOs.Auth.Response;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Account;

namespace Application.Core.Services.Auth;

public sealed class AuthenticationService : IAuthentication
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IPassword _passwordService;
    private readonly IJwt _jwtService;
    private readonly IDeterministicEncryption _deterministicEncryption;
    private readonly ITimeProvider _timeProvider;
    private readonly JwtConfig _jwtConfig;

    public AuthenticationService(
        ICredentialRepository credentialRepository,
        IPassword passwordService,
        IJwt jwtService,
        IDeterministicEncryption deterministicEncryption,
        ITimeProvider timeProvider,
        JwtConfig jwtConfig)
    {
        _credentialRepository = credentialRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _deterministicEncryption = deterministicEncryption;
        _timeProvider = timeProvider;
        _jwtConfig = jwtConfig;
    }

    public Task<Result<LoginResponse, AuthenticationError>> LoginAsync(LoginRequest request) =>
        _credentialRepository.GetByUsernameAsync(request.Username)
            .MapErrorAsync(AuthenticationError (error) =>
                new GetByUsernameAsyncDomainError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new UserNotFoundError())
            .EnsureAsync(user => user.IsActive, new UserInactiveError())
            .BindAsync(user => ValidatePasswordAndGenerateTokens(user, request.Password));

    public Task<Result<LoginResponse, AuthenticationError>> RefreshTokenAsync(RefreshTokenRequest request) =>
        EncryptTokenForLookup(request.RefreshToken)
            .BindAsync(tokenHash => _credentialRepository.GetByRefreshTokenAsync(tokenHash)
                .MapErrorAsync(AuthenticationError (error) =>
                    new GetByRefreshTokenAsyncDomainError(error.Message, error.Details, error.Exception)))
            .EnsureNotNullAsync(new RefreshTokenNotFoundError())
            .EnsureAsync(user => user.IsActive, new UserInactiveError())
            .BindAsync(GenerateAndStoreNewTokens);

    public Task<Result<Unit, AuthenticationError>> LogoutAsync(LogoutRequest request) =>
        EncryptTokenForLookup(request.RefreshToken)
            .BindAsync(tokenHash => _credentialRepository.GetByRefreshTokenAsync(tokenHash)
                .MapErrorAsync(AuthenticationError (error) =>
                    new GetByRefreshTokenAsyncDomainError(error.Message, error.Details, error.Exception)))
            .EnsureNotNullAsync(new RefreshTokenNotFoundError())
            .BindAsync(user => _credentialRepository.ClearRefreshTokenAsync(user.UserCode)
                .MapErrorAsync(AuthenticationError (error) =>
                    new GetByRefreshTokenAsyncDomainError(error.Message, error.Details, error.Exception)));

    private Task<Result<LoginResponse, AuthenticationError>> ValidatePasswordAndGenerateTokens(User user,
        string password) =>
        _passwordService.VerifyPassword(password, user.PasswordHash)
            .MapError(AuthenticationError (error) =>
                new ChaChaDecryptError(error.Message, error.Details, error.Exception))
            .Ensure(isValid => isValid, new IncorrectPasswordError())
            .BindAsync(_ => GenerateAndStoreNewTokens(user));

    private Task<Result<LoginResponse, AuthenticationError>> GenerateAndStoreNewTokens(User user)
    {
        string rawToken = _jwtService.GenerateRefreshToken();

        return _jwtService.GenerateAccessToken(user)
            .MapError(AuthenticationError (error) => error)
            .BindAsync(tokens =>
                _deterministicEncryption.Encrypt(rawToken)
                    .MapError(AuthenticationError (error) => new JwtStorageError(error.Details, error.Exception))
                    .BindAsync(tokenHash =>
                        StoreRefreshToken(user.UserCode, tokenHash)
                            .MapAsync(_ =>
                                GenerateLoginResponse(user, tokens.AccessToken, rawToken, tokens.ExpiresAt))));
    }

    private Task<Result<Unit, AuthenticationError>> StoreRefreshToken(Guid userCode, string tokenHash) =>
        _credentialRepository.UpdateRefreshTokenAsync(
                userCode,
                tokenHash,
                _timeProvider.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes))
            .MapErrorAsync(AuthenticationError (error) =>
                new StoreRefreshTokenError(error.Message, error.Details, error.Exception));

    /// <summary>
    /// Deterministically encrypts a raw token received from the client so it can be
    /// matched against the stored ciphertext in <c>RefreshToken."TokenHash"</c>.
    /// </summary>
    private Task<Result<string, AuthenticationError>> EncryptTokenForLookup(string rawToken) =>
        Task.FromResult(
            _deterministicEncryption.Encrypt(rawToken)
                .MapError(AuthenticationError (error) =>
                    new GetByRefreshTokenAsyncDomainError(error.Message, error.Details, error.Exception)));

    private static LoginResponse GenerateLoginResponse(
        User user,
        string accessToken,
        string rawRefreshToken,
        DateTime expiresAt) =>
        new()
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                UserCode = user.UserCode,
                Username = user.Username,
                FullName = $"{user.Names} {user.Surnames}",
                RoleName = user.RoleName
            }
        };
}