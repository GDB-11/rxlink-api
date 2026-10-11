using Application.Core.Config;
using Application.Core.DTOs.Account;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Auth.Request;
using Application.Core.DTOs.Auth.Response;
using Application.Core.DTOs.Mfa.Errors;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Mfa;
using Application.Core.Interfaces.Shared;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.DTOs.Account;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Activity;
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
    private readonly IActivityLogger _activityLogger;
    private readonly IMfa _mfaService;
    private readonly SessionSecurityConfig _security;

    public AuthenticationService(
        ICredentialRepository credentialRepository,
        IPassword passwordService,
        IJwt jwtService,
        IDeterministicEncryption deterministicEncryption,
        ITimeProvider timeProvider,
        JwtConfig jwtConfig,
        IActivityLogger activityLogger,
        IMfa mfaService,
        SessionSecurityConfig security)
    {
        _credentialRepository = credentialRepository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _deterministicEncryption = deterministicEncryption;
        _timeProvider = timeProvider;
        _jwtConfig = jwtConfig;
        _activityLogger = activityLogger;
        _mfaService = mfaService;
        _security = security;
    }

    public Task<Result<LoginResponse, AuthenticationError>> LoginAsync(LoginRequest request) =>
        _credentialRepository.GetByUsernameAsync(request.Username)
            .MapErrorAsync(AuthenticationError (error) =>
                new GetByUsernameAsyncDomainError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new UserNotFoundError())
            .EnsureAsync(user => user.IsActive, new UserInactiveError())
            .BindAsync(EnsureNotLocked)
            .BindAsync(user => ValidatePasswordAndGenerateTokens(user, request.Password))
            .TapAsync(response => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.LoginSucceeded, response.User.UserCode, ActivityEntityTypes.User,
                response.User.UserCode, "User logged in")))
            .TapErrorAsync(error => IsRejectedCredential(error)
                ? _activityLogger.LogAsync(new ActivityEvent(
                    ActivityEventTypes.LoginFailed, null, null, null, "Login failed",
                    new { request.Username, Reason = error.GetType().Name }))
                : Task.CompletedTask);

    public Task<Result<LoginResponse, AuthenticationError>> VerifyMfaAsync(MfaVerifyRequest request) =>
        _jwtService.ValidateMfaTokenAsync(request.MfaToken)
            .BindAsync(userCode => _credentialRepository.GetByCodeAsync(userCode)
                .MapErrorAsync(AuthenticationError (error) =>
                    new GetByUsernameAsyncDomainError(error.Message, error.Details, error.Exception)))
            .EnsureNotNullAsync(new UserNotFoundError())
            .EnsureAsync(user => user.IsActive, new UserInactiveError())
            .BindAsync(EnsureNotLocked)
            .BindAsync(user => VerifyMfaCodeAsync(user, request.Code))
            .BindAsync(CompleteLoginAsync)
            .TapAsync(response => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.LoginSucceeded, response.User.UserCode, ActivityEntityTypes.User,
                response.User.UserCode, "User logged in", new { Mfa = true })));

    public Task<Result<LoginResponse, AuthenticationError>> RefreshTokenAsync(RefreshTokenRequest request) =>
        EncryptTokenForLookup(request.RefreshToken)
            .BindAsync(tokenHash => _credentialRepository.GetByRefreshTokenAsync(tokenHash)
                .MapErrorAsync(AuthenticationError (error) =>
                    new GetByRefreshTokenAsyncDomainError(error.Message, error.Details, error.Exception)))
            .EnsureNotNullAsync(new RefreshTokenNotFoundError())
            .EnsureAsync(user => user.IsActive, new UserInactiveError())
            .EnsureAsync(user => !SessionLimitReached(user), new SessionExpiredError())
            .BindAsync(user => GenerateAndStoreNewTokens(user, user.SessionStartedAt ?? _timeProvider.UtcNow))
            .TapErrorAsync(error => error is RefreshTokenNotFoundError
                ? LogRefreshTokenReuseAsync(request.RefreshToken)
                : Task.CompletedTask);

    public Task<Result<Unit, AuthenticationError>> LogoutAsync(LogoutRequest request) =>
        EncryptTokenForLookup(request.RefreshToken)
            .BindAsync(tokenHash => _credentialRepository.GetByRefreshTokenAsync(tokenHash)
                .MapErrorAsync(AuthenticationError (error) =>
                    new GetByRefreshTokenAsyncDomainError(error.Message, error.Details, error.Exception)))
            .EnsureNotNullAsync(new RefreshTokenNotFoundError())
            .BindAsync(user => _credentialRepository.ClearRefreshTokenAsync(user.UserCode)
                .MapErrorAsync(AuthenticationError (error) =>
                    new GetByRefreshTokenAsyncDomainError(error.Message, error.Details, error.Exception))
                .TapAsync(_ => _activityLogger.LogAsync(new ActivityEvent(
                    ActivityEventTypes.Logout, user.UserCode, ActivityEntityTypes.User, user.UserCode,
                    "User logged out"))));

    /// <summary>Errors that mean the caller presented bad credentials (as opposed to an infrastructure failure).</summary>
    private static bool IsRejectedCredential(AuthenticationError error) =>
        error is UserNotFoundError or UserInactiveError or IncorrectPasswordError or AccountLockedError;

    /// <summary>
    /// Emits <c>RefreshTokenReuse</c> when the rejected refresh token belongs to a token that was already
    /// revoked (rotated or logged out). Unknown tokens are not reported.
    /// </summary>
    private Task LogRefreshTokenReuseAsync(string rawToken) =>
        EncryptTokenForLookup(rawToken)
            .BindAsync(tokenHash => _credentialRepository.GetRevokedRefreshTokenOwnerAsync(tokenHash)
                .MapErrorAsync(AuthenticationError (error) =>
                    new GetByRefreshTokenAsyncDomainError(error.Message, error.Details, error.Exception)))
            .TapAsync(ownerCode => ownerCode.HasValue
                ? _activityLogger.LogAsync(new ActivityEvent(
                    ActivityEventTypes.RefreshTokenReuse, ownerCode, ActivityEntityTypes.User, ownerCode,
                    "Revoked refresh token presented again"))
                : Task.CompletedTask);

    private async Task<Result<LoginResponse, AuthenticationError>> ValidatePasswordAndGenerateTokens(User user,
        string password)
    {
        Result<bool, AuthenticationError> check = _passwordService.VerifyPassword(password, user.PasswordHash)
            .MapError(AuthenticationError (error) =>
                new ChaChaDecryptError(error.Message, error.Details, error.Exception))
            .Ensure(isValid => isValid, new IncorrectPasswordError());

        if (check.IsFailure)
        {
            AuthenticationError error = check.Error is IncorrectPasswordError
                ? await RegisterFailedAttemptAsync(user, check.Error)
                : check.Error;
            return Result<LoginResponse, AuthenticationError>.Failure(error);
        }

        // With MFA the password alone does not complete the login, so the failure counter is kept until the code is verified.
        return user.MfaEnabled ? await IssueMfaChallenge(user) : await CompleteLoginAsync(user);
    }

    private Task<Result<User, AuthenticationError>> EnsureNotLocked(User user)
    {
        DateTime now = _timeProvider.UtcNow;
        Result<User, AuthenticationError> result = user.LockedUntil is { } lockedUntil && lockedUntil > now
            ? Result<User, AuthenticationError>.Failure(
                new AccountLockedError(lockedUntil, (int)Math.Ceiling((lockedUntil - now).TotalSeconds)))
            : Result<User, AuthenticationError>.Success(user);
        return Task.FromResult(result);
    }

    private async Task<Result<User, AuthenticationError>> VerifyMfaCodeAsync(User user, string code)
    {
        Result<Unit, MfaError> verification = await _mfaService.VerifyCodeAsync(user.UserCode, code);
        if (verification.IsSuccess) return user;

        AuthenticationError error = verification.Error switch
        {
            MfaInvalidCodeError or MfaNotEnabledError => new InvalidMfaCodeError(),
            var e => new MfaVerificationError(e.Message, e.Details, e.Exception)
        };

        if (error is InvalidMfaCodeError) error = await RegisterFailedAttemptAsync(user, error);
        return Result<User, AuthenticationError>.Failure(error);
    }

    /// <summary>
    /// Counts a wrong password / MFA code. Returns <see cref="AccountLockedError"/> when this attempt triggered the
    /// lockout, otherwise the original error. Counter failures never mask the original rejection.
    /// </summary>
    private async Task<AuthenticationError> RegisterFailedAttemptAsync(User user, AuthenticationError error)
    {
        DateTime now = _timeProvider.UtcNow;
        Result<DateTime?, CredentialError> recorded = await _credentialRepository.RecordFailedLoginAsync(
            user.UserCode, _security.MaxFailedAttempts, now, now.AddMinutes(_security.LockoutMinutes));

        if (recorded.IsFailure || recorded.Value is not { } lockedUntil || lockedUntil <= now) return error;

        await _activityLogger.LogAsync(new ActivityEvent(
            ActivityEventTypes.AccountLocked, user.UserCode, ActivityEntityTypes.User, user.UserCode,
            "Account locked after repeated failed attempts", new { _security.MaxFailedAttempts, LockedUntil = lockedUntil }));
        return new AccountLockedError(lockedUntil, (int)Math.Ceiling((lockedUntil - now).TotalSeconds));
    }

    /// <summary>Credentials fully verified: clears the failure counter and starts a new session.</summary>
    private async Task<Result<LoginResponse, AuthenticationError>> CompleteLoginAsync(User user)
    {
        Result<Unit, CredentialError> reset = await _credentialRepository.ResetFailedLoginsAsync(user.UserCode);
        if (reset.IsFailure)
            return Result<LoginResponse, AuthenticationError>.Failure(
                new StoreRefreshTokenError(reset.Error.Message, reset.Error.Details, reset.Error.Exception));

        return await GenerateAndStoreNewTokens(user, _timeProvider.UtcNow);
    }

    private bool SessionLimitReached(User user) =>
        _security.AbsoluteSessionMinutes > 0
        && user.SessionStartedAt is { } started
        && _timeProvider.UtcNow - started > TimeSpan.FromMinutes(_security.AbsoluteSessionMinutes);

    /// <summary>Password is correct but a second factor is required: no session tokens are issued yet.</summary>
    private Task<Result<LoginResponse, AuthenticationError>> IssueMfaChallenge(User user) =>
        Task.FromResult(_jwtService.GenerateMfaToken(user)
            .Bind(token => Result<LoginResponse, AuthenticationError>.Failure(
                new MfaRequiredError(token.Token, token.ExpiresAt))));

    private Task<Result<LoginResponse, AuthenticationError>> GenerateAndStoreNewTokens(User user,
        DateTime sessionStartedAt)
    {
        string rawToken = _jwtService.GenerateRefreshToken();

        return _jwtService.GenerateAccessToken(user)
            .MapError(AuthenticationError (error) => error)
            .BindAsync(tokens =>
                _deterministicEncryption.Encrypt(rawToken)
                    .MapError(AuthenticationError (error) => new JwtStorageError(error.Details, error.Exception))
                    .BindAsync(tokenHash =>
                        StoreRefreshToken(user.UserCode, tokenHash, sessionStartedAt)
                            .MapAsync(_ =>
                                GenerateLoginResponse(user, tokens.AccessToken, rawToken, tokens.ExpiresAt))));
    }

    private Task<Result<Unit, AuthenticationError>> StoreRefreshToken(Guid userCode, string tokenHash,
        DateTime sessionStartedAt) =>
        _credentialRepository.UpdateRefreshTokenAsync(
                userCode,
                tokenHash,
                _timeProvider.UtcNow.AddMinutes(_jwtConfig.RefreshTokenExpiryMinutes),
                sessionStartedAt)
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