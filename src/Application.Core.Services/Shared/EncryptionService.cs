using Application.Core.DTOs.Encryption.Errors;
using Application.Core.Interfaces.Shared;
using BindSharp;

namespace Application.Core.Services.Shared;

public sealed class EncryptionService : IEncryption
{
    private readonly IChaChaEncryption _chaChaEncryption;

    public EncryptionService(IChaChaEncryption chaChaEncryption)
    {
        _chaChaEncryption = chaChaEncryption;
    }

    public Result<string, ChaChaEncryptionError> Encrypt(string plaintext) =>
        ValidatePlaintext(plaintext)
            .Bind(_chaChaEncryption.Encrypt);

    public Result<string, ChaChaEncryptionError> Decrypt(string ciphertext) =>
        ValidateCiphertext(ciphertext)
            .Bind(_chaChaEncryption.Decrypt);

    private static Result<string, ChaChaEncryptionError> ValidatePlaintext(string plaintext) =>
        !string.IsNullOrEmpty(plaintext)
            ? plaintext
            : new ChaChaEncryptError("Plain text cannot be null or empty");

    private static Result<string, ChaChaEncryptionError> ValidateCiphertext(string ciphertext) =>
        !string.IsNullOrEmpty(ciphertext)
            ? ciphertext
            : new ChaChaDecryptError("Cipher text cannot be null or empty");
}