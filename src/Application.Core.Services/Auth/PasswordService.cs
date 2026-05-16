using Application.Core.DTOs.Encryption.Errors;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using BindSharp;

namespace Application.Core.Services.Auth;

public sealed class PasswordService : IPassword
{
    private readonly IEncryption _encryptionService;

    public PasswordService(IEncryption encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public Result<string, ChaChaEncryptionError> HashPassword(string password) =>
        _encryptionService.Encrypt(password);

    public Result<bool, ChaChaEncryptionError> VerifyPassword(string password, string passwordHash) =>
        _encryptionService.Decrypt(passwordHash)
            .Map(decryptedPassword => decryptedPassword == password);
}