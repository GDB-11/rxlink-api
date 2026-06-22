using Application.Core.DTOs.Encryption.Errors;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using BindSharp;
using NSubstitute;

namespace Application.Core.Services.Tests.Auth;

public sealed class PasswordServiceTests
{
    private readonly IEncryption _encryptionService = Substitute.For<IEncryption>();
    private readonly PasswordService _sut;

    public PasswordServiceTests() => _sut = new PasswordService(_encryptionService);

    // ── HashPassword ────────────────────────────────────────────────────────

    [Fact]
    public void HashPassword_DelegatesToEncryptionService()
    {
        const string password = "my-plain-password";
        const string hash = "encrypted-hash";
        _encryptionService.Encrypt(password)
            .Returns(Result<string, ChaChaEncryptionError>.Success(hash));

        var result = _sut.HashPassword(password);

        Assert.True(result.IsSuccess);
        Assert.Equal(hash, result.Value);
        _encryptionService.Received(1).Encrypt(password);
    }

    [Fact]
    public void HashPassword_EncryptionFails_PropagatesFailure()
    {
        _encryptionService.Encrypt(Arg.Any<string>())
            .Returns(Result<string, ChaChaEncryptionError>.Failure(
                new ChaChaEncryptError("encryption error")));

        var result = _sut.HashPassword("password");

        Assert.True(result.IsFailure);
        Assert.IsType<ChaChaEncryptError>(result.Error);
    }

    // ── VerifyPassword ──────────────────────────────────────────────────────

    [Fact]
    public void VerifyPassword_PasswordMatchesDecryptedHash_ReturnsTrue()
    {
        const string password = "correct-password";
        const string hash = "encrypted-hash";
        _encryptionService.Decrypt(hash)
            .Returns(Result<string, ChaChaEncryptionError>.Success(password));

        var result = _sut.VerifyPassword(password, hash);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void VerifyPassword_PasswordDoesNotMatchDecryptedHash_ReturnsFalse()
    {
        const string password = "wrong-password";
        const string hash = "encrypted-hash";
        _encryptionService.Decrypt(hash)
            .Returns(Result<string, ChaChaEncryptionError>.Success("correct-password"));

        var result = _sut.VerifyPassword(password, hash);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public void VerifyPassword_DecryptionFails_PropagatesFailure()
    {
        _encryptionService.Decrypt(Arg.Any<string>())
            .Returns(Result<string, ChaChaEncryptionError>.Failure(
                new PerformDecryption("decryption failed")));

        var result = _sut.VerifyPassword("password", "corrupt-hash");

        Assert.True(result.IsFailure);
        Assert.IsType<PerformDecryption>(result.Error);
    }
}
