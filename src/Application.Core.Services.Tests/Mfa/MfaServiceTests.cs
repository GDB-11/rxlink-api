using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.Mfa.Errors;
using Application.Core.DTOs.Mfa.Request;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Mfa;
using BindSharp;
using Infrastructure.Core.DTOs.Mfa;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Mfa;
using Infrastructure.Core.Models.Mfa;
using NSubstitute;

namespace Application.Core.Services.Tests.Mfa;

public sealed class MfaServiceTests
{
    private readonly IMfaRepository _repository = Substitute.For<IMfaRepository>();
    private readonly ITotp _totp = Substitute.For<ITotp>();
    private readonly IChaChaEncryption _encryption = Substitute.For<IChaChaEncryption>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly MfaService _sut;
    private readonly Guid _userCode = Guid.NewGuid();

    public MfaServiceTests()
    {
        _sut = new MfaService(_repository, _totp, _encryption, _activityLogger, _userContext);
        _encryption.Decrypt(Arg.Any<string>())
            .Returns(Result<string, ChaChaEncryptionError>.Success("SECRET"));
    }

    private void GivenRow(MfaRow? row) =>
        _repository.GetByUserCodeAsync(_userCode)
            .Returns(Task.FromResult(Result<MfaRow?, MfaRepositoryError>.Success(row)));

    private static MfaRow Row(bool confirmed, long? lastStep = null) =>
        new() { SecretEncrypted = "enc", IsConfirmed = confirmed, LastUsedStep = lastStep };

    private void GivenAdvance(int affected) =>
        _repository.AdvanceStepAsync(_userCode, Arg.Any<long>())
            .Returns(Task.FromResult(Result<int, MfaRepositoryError>.Success(affected)));

    [Fact]
    public async Task EnrollAsync_AlreadyConfirmed_ReturnsAlreadyEnabled()
    {
        GivenRow(Row(confirmed: true));

        var result = await _sut.EnrollAsync(_userCode, "dr@x.com");

        Assert.IsType<MfaAlreadyEnabledError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().UpsertPendingAsync(default, default!);
    }

    [Fact]
    public async Task EnrollAsync_NoRow_StoresEncryptedSecretAndReturnsUri()
    {
        GivenRow(null);
        _totp.GenerateSecret().Returns("PLAINSECRET");
        _totp.BuildOtpauthUri("PLAINSECRET", "RxLink", "dr@x.com").Returns("otpauth://uri");
        _encryption.Encrypt("PLAINSECRET").Returns(Result<string, ChaChaEncryptionError>.Success("cipher"));
        _repository.UpsertPendingAsync(_userCode, "cipher")
            .Returns(Task.FromResult(Result<int, MfaRepositoryError>.Success(1)));

        var result = await _sut.EnrollAsync(_userCode, "dr@x.com");

        Assert.True(result.IsSuccess);
        Assert.Equal("PLAINSECRET", result.Value.Secret);
        Assert.Equal("otpauth://uri", result.Value.OtpauthUri);
    }

    [Fact]
    public async Task ConfirmAsync_ValidCode_ConfirmsAndLogsEnrolment()
    {
        GivenRow(Row(confirmed: false));
        _totp.Validate("SECRET", "123456").Returns(10L);
        _repository.ConfirmAsync(_userCode, 10L)
            .Returns(Task.FromResult(Result<int, MfaRepositoryError>.Success(1)));

        var result = await _sut.ConfirmAsync(_userCode, new ConfirmMfaRequest { Code = "123456" });

        Assert.True(result.IsSuccess);
        await _activityLogger.Received(1).LogAsync(
            Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.MfaEnrolled));
    }

    [Fact]
    public async Task ConfirmAsync_WrongCode_ReturnsInvalidCodeAndLogsFailure()
    {
        GivenRow(Row(confirmed: false));
        _totp.Validate("SECRET", "000000").Returns((long?)null);

        var result = await _sut.ConfirmAsync(_userCode, new ConfirmMfaRequest { Code = "000000" });

        Assert.IsType<MfaInvalidCodeError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().ConfirmAsync(default, default);
        await _activityLogger.Received(1).LogAsync(
            Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.MfaFailed));
    }

    [Fact]
    public async Task ConfirmAsync_NothingPending_ReturnsNotPending()
    {
        GivenRow(null);

        var result = await _sut.ConfirmAsync(_userCode, new ConfirmMfaRequest { Code = "123456" });

        Assert.IsType<MfaNotPendingError>(result.Error);
    }

    [Fact]
    public async Task VerifyCodeAsync_ValidFreshCode_Succeeds()
    {
        GivenRow(Row(confirmed: true, lastStep: 5));
        _totp.Validate("SECRET", "123456").Returns(6L);
        GivenAdvance(1);

        var result = await _sut.VerifyCodeAsync(_userCode, "123456");

        Assert.True(result.IsSuccess);
        await _repository.Received(1).AdvanceStepAsync(_userCode, 6L);
    }

    [Fact]
    public async Task VerifyCodeAsync_StepAlreadyUsed_RejectedWithoutTouchingRepository()
    {
        GivenRow(Row(confirmed: true, lastStep: 6));
        _totp.Validate("SECRET", "123456").Returns(6L);

        var result = await _sut.VerifyCodeAsync(_userCode, "123456");

        Assert.IsType<MfaInvalidCodeError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().AdvanceStepAsync(default, default);
    }

    [Fact]
    public async Task VerifyCodeAsync_ConcurrentReplay_AdvanceAffectsNoRows_Rejected()
    {
        GivenRow(Row(confirmed: true, lastStep: 5));
        _totp.Validate("SECRET", "123456").Returns(6L);
        GivenAdvance(0);

        var result = await _sut.VerifyCodeAsync(_userCode, "123456");

        Assert.IsType<MfaInvalidCodeError>(result.Error);
    }

    [Fact]
    public async Task VerifyCodeAsync_NotEnabled_ReturnsNotEnabled()
    {
        GivenRow(Row(confirmed: false));

        var result = await _sut.VerifyCodeAsync(_userCode, "123456");

        Assert.IsType<MfaNotEnabledError>(result.Error);
    }

    [Fact]
    public async Task ResetAsync_NoMfa_ReturnsNotEnabled()
    {
        _repository.DeleteAsync(_userCode)
            .Returns(Task.FromResult(Result<int, MfaRepositoryError>.Success(0)));

        var result = await _sut.ResetAsync(_userCode);

        Assert.IsType<MfaNotEnabledError>(result.Error);
        await _activityLogger.DidNotReceiveWithAnyArgs().LogAsync(default!);
    }

    [Fact]
    public async Task ResetAsync_Existing_DeletesAndLogsReset()
    {
        _repository.DeleteAsync(_userCode)
            .Returns(Task.FromResult(Result<int, MfaRepositoryError>.Success(1)));

        var result = await _sut.ResetAsync(_userCode);

        Assert.True(result.IsSuccess);
        await _activityLogger.Received(1).LogAsync(
            Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.MfaReset && e.EntityCode == _userCode));
    }
}
