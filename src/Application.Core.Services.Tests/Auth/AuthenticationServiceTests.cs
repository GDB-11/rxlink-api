using Application.Core.Config;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Auth.Request;
using Application.Core.DTOs.Auth.Response;
// RefreshTokenRequest lives in the Response namespace (upstream convention)
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.Mfa.Errors;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Mfa;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using BindSharp;
using Infrastructure.Core.DTOs.Account;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Models.Account;
using NSubstitute;

namespace Application.Core.Services.Tests.Auth;

public sealed class AuthenticationServiceTests
{
    private readonly ICredentialRepository _credentialRepository = Substitute.For<ICredentialRepository>();
    private readonly IPassword _passwordService = Substitute.For<IPassword>();
    private readonly IJwt _jwtService = Substitute.For<IJwt>();
    private readonly IDeterministicEncryption _deterministicEncryption = Substitute.For<IDeterministicEncryption>();
    private readonly ITimeProvider _timeProvider = Substitute.For<ITimeProvider>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();

    private readonly JwtConfig _jwtConfig = new()
    {
        SecretKey = "test-secret-key-for-unit-tests",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpiryMinutes = 60,
        RefreshTokenExpiryMinutes = 10080
    };

    private readonly SessionSecurityConfig _security = new()
    {
        MaxFailedAttempts = 3,
        LockoutMinutes = 15,
        AbsoluteSessionMinutes = 600
    };

    private readonly IMfa _mfaService = Substitute.For<IMfa>();
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _sut = new AuthenticationService(
            _credentialRepository,
            _passwordService,
            _jwtService,
            _deterministicEncryption,
            _timeProvider,
            _jwtConfig,
            _activityLogger,
            _mfaService,
            _security);

        _timeProvider.UtcNow.Returns(DateTime.UtcNow);
        _credentialRepository.RecordFailedLoginAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(Result<DateTime?, CredentialError>.Success(null)));
        _credentialRepository.ResetFailedLoginsAsync(Arg.Any<Guid>())
            .Returns(Task.FromResult(Result<Unit, CredentialError>.Success(Unit.Value)));
    }

    private static User MakeUser(bool isActive = true, bool mfaEnabled = false) => new()
    {
        UserCode = Guid.NewGuid(),
        PersonCode = Guid.NewGuid(),
        RoleName = "Doctor",
        Username = "dr.garcia",
        Email = "garcia@hospital.com",
        PasswordHash = "encrypted-password",
        IsActive = isActive,
        MfaEnabled = mfaEnabled,
        CreatedAt = DateTime.UtcNow,
        Names = "Carlos",
        Surnames = "García"
    };

    private void SetupTokenGenerationHappyPath(User user)
    {
        _passwordService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<bool, ChaChaEncryptionError>.Success(true));
        _jwtService.GenerateRefreshToken().Returns("raw-refresh-token");
        _jwtService.GenerateAccessToken(Arg.Any<User>())
            .Returns(Result<(string AccessToken, DateTime ExpiresAt), AuthenticationError>.Success(
                ("access-token", DateTime.UtcNow.AddHours(1))));
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository
            .UpdateRefreshTokenAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(Result<Unit, CredentialError>.Success(Unit.Value)));
    }

    // ── MFA ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_UserWithMfa_ReturnsMfaRequiredAndIssuesNoSessionTokens()
    {
        var user = MakeUser(mfaEnabled: true);
        _credentialRepository.GetByUsernameAsync("dr.garcia")
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _passwordService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<bool, ChaChaEncryptionError>.Success(true));
        _jwtService.GenerateMfaToken(user)
            .Returns(Result<(string Token, DateTime ExpiresAt), AuthenticationError>.Success(
                ("mfa-token", DateTime.UtcNow.AddMinutes(5))));

        var result = await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "password123" });

        Assert.True(result.IsFailure);
        var challenge = Assert.IsType<MfaRequiredError>(result.Error);
        Assert.Equal("mfa-token", challenge.MfaToken);
        _jwtService.DidNotReceive().GenerateAccessToken(Arg.Any<User>());
        await _activityLogger.DidNotReceiveWithAnyArgs().LogAsync(default!);
    }

    [Fact]
    public async Task VerifyMfaAsync_ValidTokenAndCode_ReturnsLoginResponse()
    {
        var user = MakeUser(mfaEnabled: true);
        _jwtService.ValidateMfaTokenAsync("mfa-token")
            .Returns(Task.FromResult(Result<Guid, AuthenticationError>.Success(user.UserCode)));
        _mfaService.VerifyCodeAsync(user.UserCode, "123456")
            .Returns(Task.FromResult(Result<Unit, MfaError>.Success(Unit.Value)));
        _credentialRepository.GetByCodeAsync(user.UserCode)
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        SetupTokenGenerationHappyPath(user);

        var result = await _sut.VerifyMfaAsync(new MfaVerifyRequest { MfaToken = "mfa-token", Code = "123456" });

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
    }

    [Fact]
    public async Task VerifyMfaAsync_WrongCode_ReturnsInvalidMfaCodeError()
    {
        var user = MakeUser(mfaEnabled: true);
        var userCode = user.UserCode;
        _jwtService.ValidateMfaTokenAsync("mfa-token")
            .Returns(Task.FromResult(Result<Guid, AuthenticationError>.Success(userCode)));
        _credentialRepository.GetByCodeAsync(userCode)
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _mfaService.VerifyCodeAsync(userCode, "000000")
            .Returns(Task.FromResult(Result<Unit, MfaError>.Failure(new MfaInvalidCodeError())));

        var result = await _sut.VerifyMfaAsync(new MfaVerifyRequest { MfaToken = "mfa-token", Code = "000000" });

        Assert.True(result.IsFailure);
        Assert.IsType<InvalidMfaCodeError>(result.Error);
        _jwtService.DidNotReceive().GenerateAccessToken(Arg.Any<User>());
    }

    [Fact]
    public async Task VerifyMfaAsync_InvalidToken_ReturnsInvalidMfaTokenError()
    {
        _jwtService.ValidateMfaTokenAsync("bad")
            .Returns(Task.FromResult(Result<Guid, AuthenticationError>.Failure(new InvalidMfaTokenError())));

        var result = await _sut.VerifyMfaAsync(new MfaVerifyRequest { MfaToken = "bad", Code = "123456" });

        Assert.IsType<InvalidMfaTokenError>(result.Error);
        await _mfaService.DidNotReceiveWithAnyArgs().VerifyCodeAsync(default, default!);
    }

    // ── LoginAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        var user = MakeUser();
        _credentialRepository.GetByUsernameAsync("dr.garcia")
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        SetupTokenGenerationHappyPath(user);

        var result = await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "password123" });

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("raw-refresh-token", result.Value.RefreshToken);
        Assert.Equal(user.UserCode, result.Value.User.UserCode);
        Assert.Equal(user.Username, result.Value.User.Username);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(null)));

        var result = await _sut.LoginAsync(new LoginRequest { Username = "unknown", Password = "password" });

        Assert.True(result.IsFailure);
        Assert.IsType<UserNotFoundError>(result.Error);
    }

    [Fact]
    public async Task LoginAsync_UserInactive_ReturnsUserInactiveError()
    {
        var user = MakeUser(isActive: false);
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));

        var result = await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "password" });

        Assert.True(result.IsFailure);
        Assert.IsType<UserInactiveError>(result.Error);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsIncorrectPasswordError()
    {
        var user = MakeUser();
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _passwordService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<bool, ChaChaEncryptionError>.Success(false));

        var result = await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "wrong-password" });

        Assert.True(result.IsFailure);
        Assert.IsType<IncorrectPasswordError>(result.Error);
    }

    [Fact]
    public async Task LoginAsync_RepositoryFails_ReturnsFailure()
    {
        // BindSharp 2.1.0 EnsureAsync throws accessing Value on any reference-type Failure,
        // converting the error to UserInactiveError — only IsFailure can be asserted.
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(
                Result<User?, CredentialError>.Failure(new GetByUsernameAsyncError())));

        var result = await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "password" });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task LoginAsync_JwtGenerationFails_ReturnsJwtGenerationError()
    {
        var user = MakeUser();
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _passwordService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<bool, ChaChaEncryptionError>.Success(true));
        _jwtService.GenerateRefreshToken().Returns("raw-token");
        _jwtService.GenerateAccessToken(Arg.Any<User>())
            .Returns(Result<(string AccessToken, DateTime ExpiresAt), AuthenticationError>.Failure(
                new JwtGenerationError("key too short")));

        var result = await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "password" });

        Assert.True(result.IsFailure);
        Assert.IsType<JwtGenerationError>(result.Error);
    }

    // ── RefreshTokenAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewLoginResponse()
    {
        var user = MakeUser();
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync("encrypted-token-hash")
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        SetupTokenGenerationHappyPath(user);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "raw-token" });

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenNotFound_ReturnsFailure()
    {
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(null)));
        _credentialRepository.GetRevokedRefreshTokenOwnerAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<Guid?, CredentialError>.Success(null)));

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "stale-token" });

        Assert.True(result.IsFailure);
        Assert.IsType<RefreshTokenNotFoundError>(result.Error);
    }

    [Fact]
    public async Task RefreshTokenAsync_EncryptionFails_ReturnsFailure()
    {
        // BindSharp 2.1.0 EnsureAsync overwrites any upstream error — only IsFailure can be asserted.
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Failure(
                new AesEncryptionError()));

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "raw-token" });

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task RefreshTokenAsync_InactiveUser_ReturnsUserInactiveError()
    {
        var user = MakeUser(isActive: false);
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "raw-token" });

        Assert.True(result.IsFailure);
        Assert.IsType<UserInactiveError>(result.Error);
    }

    // ── Activity events ─────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_LogsLoginSucceeded()
    {
        var user = MakeUser();
        _credentialRepository.GetByUsernameAsync("dr.garcia")
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        SetupTokenGenerationHappyPath(user);

        await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "password123" });

        await _activityLogger.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.LoginSucceeded && e.ActorUserCode == user.UserCode));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_LogsLoginFailedWithoutSuccess()
    {
        var user = MakeUser();
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _passwordService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<bool, ChaChaEncryptionError>.Success(false));

        await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "wrong-password" });

        await _activityLogger.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.LoginFailed));
        await _activityLogger.DidNotReceive().LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.LoginSucceeded));
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_LogsLoginFailed()
    {
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(null)));

        await _sut.LoginAsync(new LoginRequest { Username = "unknown", Password = "password" });

        await _activityLogger.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.LoginFailed));
    }

    [Fact]
    public async Task LoginAsync_RepositoryFails_LogsNothing()
    {
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Failure(
                new GetByUsernameAsyncError("db down", new Exception("db down")))));

        await _sut.LoginAsync(new LoginRequest { Username = "dr.garcia", Password = "password" });

        await _activityLogger.DidNotReceiveWithAnyArgs().LogAsync(default!);
    }

    [Fact]
    public async Task LogoutAsync_ValidToken_LogsLogout()
    {
        var user = MakeUser();
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync("encrypted-token-hash")
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _credentialRepository.ClearRefreshTokenAsync(user.UserCode)
            .Returns(Task.FromResult(Result<Unit, CredentialError>.Success(Unit.Value)));

        await _sut.LogoutAsync(new LogoutRequest { RefreshToken = "raw-token" });

        await _activityLogger.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.Logout && e.ActorUserCode == user.UserCode));
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedTokenPresented_LogsRefreshTokenReuse()
    {
        var owner = Guid.NewGuid();
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(null)));
        _credentialRepository.GetRevokedRefreshTokenOwnerAsync("encrypted-token-hash")
            .Returns(Task.FromResult(Result<Guid?, CredentialError>.Success(owner)));

        await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "rotated-token" });

        await _activityLogger.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.RefreshTokenReuse && e.ActorUserCode == owner));
    }

    [Fact]
    public async Task RefreshTokenAsync_UnknownToken_LogsNothing()
    {
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(null)));
        _credentialRepository.GetRevokedRefreshTokenOwnerAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<Guid?, CredentialError>.Success(null)));

        await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "garbage" });

        await _activityLogger.DidNotReceiveWithAnyArgs().LogAsync(default!);
    }

    // ── LogoutAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_ValidToken_ClearsRefreshToken()
    {
        var user = MakeUser();
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync("encrypted-token-hash")
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _credentialRepository.ClearRefreshTokenAsync(user.UserCode)
            .Returns(Task.FromResult(Result<Unit, CredentialError>.Success(Unit.Value)));

        var result = await _sut.LogoutAsync(new LogoutRequest { RefreshToken = "raw-token" });

        Assert.True(result.IsSuccess);
        await _credentialRepository.Received(1).ClearRefreshTokenAsync(user.UserCode);
    }

    [Fact]
    public async Task LogoutAsync_TokenNotFound_ReturnsRefreshTokenNotFoundError()
    {
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(null)));

        var result = await _sut.LogoutAsync(new LogoutRequest { RefreshToken = "stale-token" });

        Assert.True(result.IsFailure);
        Assert.IsType<RefreshTokenNotFoundError>(result.Error);
        await _credentialRepository.DidNotReceive().ClearRefreshTokenAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task LogoutAsync_ClearFails_ReturnsGetByRefreshTokenAsyncDomainError()
    {
        var user = MakeUser();
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _credentialRepository.ClearRefreshTokenAsync(user.UserCode)
            .Returns(Task.FromResult(
                Result<Unit, CredentialError>.Failure(new ClearRefreshTokenAsyncError())));

        var result = await _sut.LogoutAsync(new LogoutRequest { RefreshToken = "raw-token" });

        Assert.True(result.IsFailure);
        Assert.IsType<GetByRefreshTokenAsyncDomainError>(result.Error);
    }

    // ── Lockout and session limits (2b.3) ───────────────────────────────────

    private void SetupUserLookup(User user) =>
        _credentialRepository.GetByUsernameAsync(user.Username)
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));

    private void SetupPassword(bool valid) =>
        _passwordService.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<bool, ChaChaEncryptionError>.Success(valid));

    [Fact]
    public async Task LoginAsync_LockedUser_ReturnsAccountLockedWithoutCheckingPassword()
    {
        var user = WithLock(MakeUser(), DateTime.UtcNow.AddMinutes(10));
        SetupUserLookup(user);

        var result = await _sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "whatever" });

        var locked = Assert.IsType<AccountLockedError>(result.Error);
        Assert.InRange(locked.RetryAfterSeconds, 1, 660);
        _passwordService.DidNotReceiveWithAnyArgs().VerifyPassword(default!, default!);
        await _credentialRepository.DidNotReceiveWithAnyArgs()
            .RecordFailedLoginAsync(default, default, default, default);
    }

    [Fact]
    public async Task LoginAsync_ExpiredLock_AllowsLogin()
    {
        var user = WithLock(MakeUser(), DateTime.UtcNow.AddMinutes(-1));
        SetupUserLookup(user);
        SetupTokenGenerationHappyPath(user);

        var result = await _sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "pw" });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_RecordsFailedAttempt()
    {
        var user = MakeUser();
        SetupUserLookup(user);
        SetupPassword(false);

        var result = await _sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "bad" });

        Assert.IsType<IncorrectPasswordError>(result.Error);
        await _credentialRepository.Received(1)
            .RecordFailedLoginAsync(user.UserCode, 3, Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task LoginAsync_AttemptReachingLimit_ReturnsAccountLockedAndLogsEvent()
    {
        var user = MakeUser();
        SetupUserLookup(user);
        SetupPassword(false);
        DateTime until = DateTime.UtcNow.AddMinutes(15);
        _credentialRepository.RecordFailedLoginAsync(user.UserCode, Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(Result<DateTime?, CredentialError>.Success(until)));

        var result = await _sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "bad" });

        Assert.IsType<AccountLockedError>(result.Error);
        await _activityLogger.Received(1).LogAsync(Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.AccountLocked));
    }

    [Fact]
    public async Task LoginAsync_Success_ResetsFailedAttemptsAndStartsSession()
    {
        var user = MakeUser();
        SetupUserLookup(user);
        SetupTokenGenerationHappyPath(user);

        var result = await _sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "pw" });

        Assert.True(result.IsSuccess);
        await _credentialRepository.Received(1).ResetFailedLoginsAsync(user.UserCode);
    }

    [Fact]
    public async Task LoginAsync_PasswordOkWithMfa_DoesNotResetFailedAttempts()
    {
        var user = MakeUser(mfaEnabled: true);
        SetupUserLookup(user);
        SetupPassword(true);
        _jwtService.GenerateMfaToken(user)
            .Returns(Result<(string Token, DateTime ExpiresAt), AuthenticationError>.Success(
                ("mfa-token", DateTime.UtcNow.AddMinutes(5))));

        var result = await _sut.LoginAsync(new LoginRequest { Username = user.Username, Password = "pw" });

        Assert.IsType<MfaRequiredError>(result.Error);
        await _credentialRepository.DidNotReceiveWithAnyArgs().ResetFailedLoginsAsync(default);
    }

    [Fact]
    public async Task VerifyMfaAsync_WrongCode_RecordsFailedAttempt()
    {
        var user = MakeUser(mfaEnabled: true);
        _jwtService.ValidateMfaTokenAsync("mfa-token")
            .Returns(Task.FromResult(Result<Guid, AuthenticationError>.Success(user.UserCode)));
        _credentialRepository.GetByCodeAsync(user.UserCode)
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        _mfaService.VerifyCodeAsync(user.UserCode, "000000")
            .Returns(Task.FromResult(Result<Unit, MfaError>.Failure(new MfaInvalidCodeError())));

        var result = await _sut.VerifyMfaAsync(new MfaVerifyRequest { MfaToken = "mfa-token", Code = "000000" });

        Assert.IsType<InvalidMfaCodeError>(result.Error);
        await _credentialRepository.Received(1)
            .RecordFailedLoginAsync(user.UserCode, 3, Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task VerifyMfaAsync_LockedUser_RejectsWithoutVerifyingCode()
    {
        var user = WithLock(MakeUser(mfaEnabled: true), DateTime.UtcNow.AddMinutes(10));
        _jwtService.ValidateMfaTokenAsync("mfa-token")
            .Returns(Task.FromResult(Result<Guid, AuthenticationError>.Success(user.UserCode)));
        _credentialRepository.GetByCodeAsync(user.UserCode)
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));

        var result = await _sut.VerifyMfaAsync(new MfaVerifyRequest { MfaToken = "mfa-token", Code = "123456" });

        Assert.IsType<AccountLockedError>(result.Error);
        await _mfaService.DidNotReceiveWithAnyArgs().VerifyCodeAsync(default, default!);
    }

    [Fact]
    public async Task RefreshTokenAsync_SessionOlderThanAbsoluteLimit_ReturnsSessionExpired()
    {
        var user = WithSessionStart(MakeUser(), DateTime.UtcNow.AddMinutes(-601));
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync("encrypted-token-hash")
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "raw-token" });

        Assert.IsType<SessionExpiredError>(result.Error);
        _jwtService.DidNotReceive().GenerateAccessToken(Arg.Any<User>());
    }

    [Fact]
    public async Task RefreshTokenAsync_WithinLimit_KeepsOriginalSessionStart()
    {
        DateTime started = DateTime.UtcNow.AddMinutes(-100);
        var user = WithSessionStart(MakeUser(), started);
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync("encrypted-token-hash")
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(user)));
        SetupTokenGenerationHappyPath(user);

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "raw-token" });

        Assert.True(result.IsSuccess);
        await _credentialRepository.Received(1)
            .UpdateRefreshTokenAsync(user.UserCode, Arg.Any<string>(), Arg.Any<DateTime>(), started);
        await _credentialRepository.DidNotReceiveWithAnyArgs().ResetFailedLoginsAsync(default);
    }

    private static User WithLock(User u, DateTime until) => Clone(u, lockedUntil: until);
    private static User WithSessionStart(User u, DateTime started) => Clone(u, sessionStartedAt: started);

    private static User Clone(User u, DateTime? lockedUntil = null, DateTime? sessionStartedAt = null) => new()
    {
        UserCode = u.UserCode, PersonCode = u.PersonCode, RoleName = u.RoleName, Username = u.Username,
        Email = u.Email, PasswordHash = u.PasswordHash, IsActive = u.IsActive, MfaEnabled = u.MfaEnabled,
        CreatedAt = u.CreatedAt, Names = u.Names, Surnames = u.Surnames,
        LockedUntil = lockedUntil, SessionStartedAt = sessionStartedAt
    };
}
