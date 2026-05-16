using System.Security.Cryptography;
using System.Text;
using Application.Core.Config;
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.Interfaces.Shared;
using BindSharp;

namespace Application.Core.Services.Shared;

public sealed class ChaChaEncryptionService : IChaChaEncryption
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _masterKey;

    public ChaChaEncryptionService(EncryptionConfig config)
    {
        _masterKey = Convert.FromBase64String(config.MasterKey);

        if (_masterKey.Length != 32)
        {
            throw new CryptographicException(
                $"Invalid key size. ChaCha20 requires a 256-bit (32 bytes) key, but received {_masterKey.Length} bytes.");
        }
    }

    public Result<string, ChaChaEncryptionError> Encrypt(string plaintext) =>
        Result.Try(
            () => Encoding.UTF8.GetBytes(plaintext),
            ChaChaEncryptionError (ex) => new GetBytesError(ex.Message, ex)
        )
        .Bind(EncryptToBytes)
        .Map(Convert.ToBase64String);

    public Result<string, ChaChaEncryptionError> Encrypt(byte[] byteContent) =>
        EncryptToBytes(byteContent)
            .Map(Convert.ToBase64String);

    public Result<byte[], ChaChaEncryptionError> EncryptToBytes(string plaintext) =>
        Result.Try(
            () => Encoding.UTF8.GetBytes(plaintext),
            ChaChaEncryptionError (ex) => new GetBytesError(ex.Message, ex)
        )
        .Bind(EncryptToBytes);

    public Result<byte[], ChaChaEncryptionError> EncryptToBytes(byte[] byteContent) =>
        Result.Try(
            () => PerformEncryption(byteContent),
            ChaChaEncryptionError (ex) => new ChaChaEncryptError(ex.Message, ex)
        );

    private byte[] PerformEncryption(byte[] byteContent)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ciphertext = new byte[byteContent.Length];
        byte[] tag = new byte[TagSize];

        using ChaCha20Poly1305 chaCha20Poly1305 = new(_masterKey);
        chaCha20Poly1305.Encrypt(nonce, byteContent, ciphertext, tag);

        return CombineEncryptedParts(nonce, tag, ciphertext);
    }

    private static byte[] CombineEncryptedParts(byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
        return result;
    }

    public Result<string, ChaChaEncryptionError> Decrypt(string ciphertext) =>
        Result.Try(
            () => Convert.FromBase64String(ciphertext),
            ChaChaEncryptionError (ex) => new GetBytesFromBase64StringError(ex.Message, ex)
        )
        .Bind(DecryptToBytes)
        .Bind(bytes => Result.Try(
            () => Encoding.UTF8.GetString(bytes),
            ChaChaEncryptionError (ex) => new GetBytesError(ex.Message, ex)
        ));

    public Result<string, ChaChaEncryptionError> Decrypt(byte[] ciphertext) =>
        DecryptToBytes(ciphertext)
            .Bind(bytes => Result.Try(
                () => Encoding.UTF8.GetString(bytes),
                ChaChaEncryptionError (ex) => new GetBytesError(ex.Message, ex)
            ));

    public Result<byte[], ChaChaEncryptionError> DecryptToBytes(string ciphertext) =>
        Result.Try(
            () => Convert.FromBase64String(ciphertext),
            ChaChaEncryptionError (ex) => new GetBytesFromBase64StringError(ex.Message, ex)
        )
        .Bind(DecryptToBytes);

    public Result<byte[], ChaChaEncryptionError> DecryptToBytes(byte[] ciphertext) =>
        ValidateCiphertextLength(ciphertext)
            .Bind(ExtractEncryptedParts)
            .Bind(PerformDecryption);

    private static Result<byte[], ChaChaEncryptionError> ValidateCiphertextLength(byte[] ciphertext)
    {
        const int minLength = NonceSize + TagSize;
        return ciphertext.Length >= minLength
            ? ciphertext
            : Result<byte[], ChaChaEncryptionError>.Failure(
                new ChaChaDecryptError($"Ciphertext too short. Expected at least {minLength} bytes, got {ciphertext.Length}"));
    }

    private static Result<(byte[] Nonce, byte[] Tag, byte[] EncryptedData), ChaChaEncryptionError> ExtractEncryptedParts(byte[] ciphertext) =>
        Result.Try(() =>
                {
                    byte[] nonce = new byte[NonceSize];
                    byte[] tag = new byte[TagSize];
                    byte[] encryptedData = new byte[ciphertext.Length - NonceSize - TagSize];
                    
                    Buffer.BlockCopy(ciphertext, 0, nonce, 0, NonceSize);
                    Buffer.BlockCopy(ciphertext, NonceSize, tag, 0, TagSize);
                    Buffer.BlockCopy(ciphertext, NonceSize + TagSize, encryptedData, 0, encryptedData.Length);
                    
                    return (nonce, tag, encryptedData);
                },
                ChaChaEncryptionError (ex) => new ExtractEncryptedPartsError(ex.Message, ex));

    private Result<byte[], ChaChaEncryptionError> PerformDecryption(
        (byte[] Nonce, byte[] Tag, byte[] EncryptedData) parts) =>
        Result.Try(() =>
            {
                byte[] plaintext = new byte[parts.EncryptedData.Length];
                using ChaCha20Poly1305 chaCha20Poly1305 = new(_masterKey);
                chaCha20Poly1305.Decrypt(parts.Nonce, parts.EncryptedData, parts.Tag, plaintext);
                return plaintext;
                
            }, 
            ChaChaEncryptionError (ex) => new PerformDecryption(ex.Message, ex));
}