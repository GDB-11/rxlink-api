using Application.Core.Config;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Auth.Request;
using Application.Core.DTOs.Auth.Response;
// RefreshTokenRequest lives in the Response namespace (upstream convention)
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using BindSharp;
using Infrastructure.Core.DTOs.Account;
using Infrastructure.Core.Interfaces.Account;
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

    private readonly JwtConfig _jwtConfig = new()
    {
        SecretKey = "test-secret-key-for-unit-tests",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpiryMinutes = 60,
        RefreshTokenExpiryMinutes = 10080
    };

    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _sut = new AuthenticationService(
            _credentialRepository,
            _passwordService,
            _jwtService,
            _deterministicEncryption,
            _timeProvider,
            _jwtConfig);

        _timeProvider.UtcNow.Returns(DateTime.UtcNow);
    }

    private static User MakeUser(bool isActive = true) => new()
    {
        UserCode = Guid.NewGuid(),
        PersonCode = Guid.NewGuid(),
        RoleName = "Doctor",
        Username = "dr.garcia",
        Email = "garcia@hospital.com",
        PasswordHash = "encrypted-password",
        IsActive = isActive,
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
            .UpdateRefreshTokenAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(Result<Unit, CredentialError>.Success(Unit.Value)));
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
        // EnsureNotNullAsync detects null → Failure(UserNotFoundError).
        // But BindSharp 2.1.0 EnsureAsync throws accessing Value on that Failure,
        // converting it to UserInactiveError — only IsFailure can be asserted.
        _credentialRepository.GetByUsernameAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(null)));

        var result = await _sut.LoginAsync(new LoginRequest { Username = "unknown", Password = "password" });

        Assert.True(result.IsFailure);
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
        // EnsureNotNullAsync detects null → Failure(RefreshTokenNotFoundError).
        // BindSharp 2.1.0 EnsureAsync then overwrites it — only IsFailure can be asserted.
        _deterministicEncryption.Encrypt(Arg.Any<string>())
            .Returns(Result<string, DeterministicEncryptionError>.Success("encrypted-token-hash"));
        _credentialRepository.GetByRefreshTokenAsync(Arg.Any<string>())
            .Returns(Task.FromResult(Result<User?, CredentialError>.Success(null)));

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "stale-token" });

        Assert.True(result.IsFailure);
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
}
