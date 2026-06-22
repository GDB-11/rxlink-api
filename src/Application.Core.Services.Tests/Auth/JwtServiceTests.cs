using Application.Core.Config;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using Infrastructure.Core.Models.Account;
using Infrastructure.Core.Models.PatientAuth;
using NSubstitute;

namespace Application.Core.Services.Tests.Auth;

public sealed class JwtServiceTests
{
    private readonly ITimeProvider _timeProvider = Substitute.For<ITimeProvider>();

    private readonly JwtConfig _jwtConfig = new()
    {
        // Key must be ≥ 256 bits (32 bytes) for Microsoft.IdentityModel HMAC-SHA256
        SecretKey = "12345678901234567890123456789012",
        Issuer = "rxlink-test",
        Audience = "rxlink-test-client",
        AccessTokenExpiryMinutes = 60,
        RefreshTokenExpiryMinutes = 10080
    };

    private readonly JwtService _sut;
    private readonly DateTime _fixedNow = new(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);

    public JwtServiceTests()
    {
        _timeProvider.UtcNow.Returns(_fixedNow);
        _sut = new JwtService(_jwtConfig, _timeProvider);
    }

    private static User MakeUser(string? licenseNumber = null) => new()
    {
        UserCode = Guid.NewGuid(),
        PersonCode = Guid.NewGuid(),
        RoleName = "Doctor",
        Username = "dr.garcia",
        Email = "garcia@hospital.com",
        PasswordHash = "hash",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        Names = "Carlos",
        Surnames = "García",
        LicenseNumber = licenseNumber
    };

    // ── GenerateAccessToken ─────────────────────────────────────────────────

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsSuccessWithNonEmptyToken()
    {
        var result = _sut.GenerateAccessToken(MakeUser());

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAtEqualsNowPlusConfiguredMinutes()
    {
        var result = _sut.GenerateAccessToken(MakeUser());

        Assert.True(result.IsSuccess);
        Assert.Equal(_fixedNow.AddMinutes(_jwtConfig.AccessTokenExpiryMinutes), result.Value.ExpiresAt);
    }

    [Fact]
    public void GenerateAccessToken_UserWithLicenseNumber_Succeeds()
    {
        var result = _sut.GenerateAccessToken(MakeUser(licenseNumber: "MED-12345"));

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
    }

    [Fact]
    public void GenerateAccessToken_UserWithoutLicenseNumber_Succeeds()
    {
        var result = _sut.GenerateAccessToken(MakeUser(licenseNumber: null));

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
    }

    [Fact]
    public void GenerateAccessToken_TwoDifferentUsers_ProduceDifferentTokens()
    {
        var result1 = _sut.GenerateAccessToken(MakeUser());
        var result2 = _sut.GenerateAccessToken(MakeUser());

        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotEqual(result1.Value.AccessToken, result2.Value.AccessToken);
    }

    // ── GeneratePatientAccessToken ──────────────────────────────────────────

    [Fact]
    public void GeneratePatientAccessToken_ValidPatient_ReturnsSuccessWithNonEmptyToken()
    {
        var patient = new PatientCredential
        {
            PatientCode = Guid.NewGuid(),
            PersonCode = Guid.NewGuid(),
            Email = "patient@mail.com",
            Names = "Ana",
            Surnames = "Torres",
            MedicalRecordNumber = "MRN-001",
            IsActive = true
        };

        var result = _sut.GeneratePatientAccessToken(patient);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.Equal(_fixedNow.AddMinutes(_jwtConfig.AccessTokenExpiryMinutes), result.Value.ExpiresAt);
    }

    // ── GenerateRefreshToken ────────────────────────────────────────────────

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var token = _sut.GenerateRefreshToken();

        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateRefreshToken_CalledTwice_ReturnsDifferentValues()
    {
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateRefreshToken_IsValidBase64()
    {
        var token = _sut.GenerateRefreshToken();

        var decoded = Convert.FromBase64String(token);
        Assert.Equal(32, decoded.Length);
    }
}
