using Application.Core.Config;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.PatientAuth.Errors;
using Application.Core.DTOs.PatientAuth.Request;
using Application.Core.DTOs.PatientAuth.Response;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Catalog;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.PatientAuth;
using BindSharp;
using Infrastructure.Core.DTOs.PatientAuth;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.PatientAuth;
using Infrastructure.Core.Models.PatientAuth;
using NSubstitute;

namespace Application.Core.Services.Tests.PatientAuth;

public sealed class PatientAuthenticationServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    private readonly IPatientCredentialRepository _repository = Substitute.For<IPatientCredentialRepository>();
    private readonly IPassword _password = Substitute.For<IPassword>();
    private readonly IJwt _jwt = Substitute.For<IJwt>();
    private readonly IDeterministicEncryption _encryption = Substitute.For<IDeterministicEncryption>();
    private readonly ITimeProvider _time = Substitute.For<ITimeProvider>();
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();
    private readonly PatientAuthenticationService _sut;

    private readonly JwtConfig _config = new()
    {
        SecretKey = "test-secret-key-for-unit-tests",
        Issuer = "test",
        Audience = "test",
        AccessTokenExpiryMinutes = 30,
        RefreshTokenExpiryMinutes = 60,
        PatientAccessTokenExpiryMinutes = 15,
        PatientRefreshTokenExpiryMinutes = 14 * 24 * 60
    };

    public PatientAuthenticationServiceTests()
    {
        _sut = new PatientAuthenticationService(
            _repository, _password, _jwt, _encryption, _time, _config,
            Substitute.For<ICatalogWriteValidator>(), _activity);

        _time.UtcNow.Returns(Now);
        _encryption.Encrypt(Arg.Any<string>())
            .Returns(call => Result<string, DeterministicEncryptionError>.Success("enc:" + call.Arg<string>()));
        _jwt.GenerateRefreshToken().Returns("new-raw");
        _jwt.GeneratePatientAccessToken(Arg.Any<PatientCredential>())
            .Returns(Result<(string AccessToken, DateTime ExpiresAt), AuthenticationError>.Success(
                ("access", Now.AddMinutes(15))));
    }

    private static PatientCredential MakePatient() => new()
    {
        PatientCode = Guid.NewGuid(),
        PersonCode = Guid.NewGuid(),
        Email = "paciente@example.test",
        Names = "Ana",
        Surnames = "Prueba",
        MedicalRecordNumber = "PAC-202610-00001",
        PasswordHash = "hash",
        IsActive = true
    };

    private static Result<T, PatientCredentialError> Ok<T>(T value) => Result<T, PatientCredentialError>.Success(value);

    [Fact]
    public async Task Refresh_RotatesTheToken_AndStoresTheSuccessorWithTheD31Lifetime()
    {
        PatientCredential patient = MakePatient();
        _repository.RotateRefreshTokenAsync("enc:old-raw", "enc:new-raw", Arg.Any<DateTime>(), Now)
            .Returns(Ok<PatientCredential?>(patient));

        var result = await _sut.RefreshAsync(new PatientRefreshRequest { RefreshToken = "old-raw" });

        Assert.True(result.IsSuccess);
        Assert.Equal("new-raw", result.Value.RefreshToken);
        await _repository.Received(1).RotateRefreshTokenAsync(
            "enc:old-raw", "enc:new-raw", Now.AddDays(14), Now);
        await _repository.DidNotReceiveWithAnyArgs().RevokeFamilyOfUsedTokenAsync(default!, default);
    }

    [Fact]
    public async Task Refresh_WithAReplayedToken_RevokesTheFamilyAndRecordsTheReuse()
    {
        _repository.RotateRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Ok<PatientCredential?>(null));
        _repository.RevokeFamilyOfUsedTokenAsync("enc:old-raw", Now).Returns(Ok(2));

        var result = await _sut.RefreshAsync(new PatientRefreshRequest { RefreshToken = "old-raw" });

        Assert.False(result.IsSuccess);
        Assert.IsType<PatientRefreshTokenNotFoundError>(result.Error);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.RefreshTokenReuse));
    }

    [Fact]
    public async Task Refresh_WithAnUnknownOrExpiredToken_FailsWithoutRecordingAReuse()
    {
        _repository.RotateRefreshTokenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Ok<PatientCredential?>(null));
        _repository.RevokeFamilyOfUsedTokenAsync(Arg.Any<string>(), Arg.Any<DateTime>()).Returns(Ok(0));

        var result = await _sut.RefreshAsync(new PatientRefreshRequest { RefreshToken = "nope" });

        Assert.IsType<PatientRefreshTokenNotFoundError>(result.Error);
        await _activity.DidNotReceiveWithAnyArgs().LogAsync(default!);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_AnswersLikeAWrongPassword()
    {
        _repository.GetByEmailAsync(Arg.Any<string>()).Returns(Ok<PatientCredential?>(null));

        var result = await _sut.LoginAsync(new PatientLoginRequest { Email = "x@example.test", Password = "irrelevant" });

        Assert.IsType<PatientIncorrectPasswordError>(result.Error);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.PatientLoginFailed));
    }

    [Fact]
    public async Task Login_WithAccountWithoutPassword_AnswersLikeAWrongPassword()
    {
        _repository.GetByEmailAsync(Arg.Any<string>()).Returns(Ok<PatientCredential?>(new PatientCredential
        {
            PatientCode = Guid.NewGuid(), PersonCode = Guid.NewGuid(), Email = "x@example.test",
            Names = "Ana", Surnames = "Prueba", MedicalRecordNumber = "PAC-202610-00002",
            PasswordHash = null, IsActive = true
        }));

        var result = await _sut.LoginAsync(new PatientLoginRequest { Email = "x@example.test", Password = "irrelevant" });

        Assert.IsType<PatientIncorrectPasswordError>(result.Error);
    }

    [Fact]
    public async Task Login_StartsANewSessionFamily()
    {
        PatientCredential patient = MakePatient();
        _repository.GetByEmailAsync(Arg.Any<string>()).Returns(Ok<PatientCredential?>(patient));
        _password.VerifyPassword(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<bool, ChaChaEncryptionError>.Success(true));
        _repository.StoreRefreshTokenAsync(default, default, default!, default, default)
            .ReturnsForAnyArgs(Ok(Unit.Value));

        var result = await _sut.LoginAsync(new PatientLoginRequest { Email = patient.Email, Password = "ok" });

        Assert.True(result.IsSuccess);
        await _repository.Received(1).StoreRefreshTokenAsync(
            patient.PatientCode, Arg.Is<Guid>(g => g != Guid.Empty), "enc:new-raw", Now.AddDays(14), Now);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.PatientLoginSucceeded));
    }

    [Fact]
    public async Task Lookup_WithNoMatch_ReturnsNotFoundAndRecordsIt()
    {
        _repository.GetByDocumentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Ok<PatientRegistrationCheckRow?>(null));

        var result = await _sut.LookupAsync(new PatientLookupRequest
        {
            DocumentTypeCode = Guid.NewGuid(), DocumentNumber = "12345678", BirthDate = new DateOnly(1990, 1, 1)
        });

        Assert.False(result.Value.Found);
        Assert.Null(result.Value.PersonCode);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.PatientLookupFailed));
    }

    [Fact]
    public async Task Lookup_WithMatch_ReturnsOnlyTheCodeAndWhetherAnAccountExists()
    {
        Guid personCode = Guid.NewGuid();
        _repository.GetByDocumentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Ok<PatientRegistrationCheckRow?>(new PatientRegistrationCheckRow
            {
                PersonCode = personCode, Names = "Ana", Surnames = "Prueba", Email = "a@example.test",
                PatientCode = Guid.NewGuid(), HasCredentials = true
            }));

        var result = await _sut.LookupAsync(new PatientLookupRequest
        {
            DocumentTypeCode = Guid.NewGuid(), DocumentNumber = "12345678", BirthDate = new DateOnly(1990, 1, 1)
        });

        Assert.True(result.Value.Found);
        Assert.Equal(personCode, result.Value.PersonCode);
        Assert.True(result.Value.HasAccount);
    }

    [Fact]
    public async Task Logout_RevokesEverySessionOfThePatient()
    {
        Guid patientCode = Guid.NewGuid();
        _repository.RevokeAllSessionsAsync(patientCode, Now).Returns(Ok(Unit.Value));

        var result = await _sut.LogoutAsync(patientCode);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).RevokeAllSessionsAsync(patientCode, Now);
    }
}
