using System.Security.Cryptography;
using System.Text;
using Application.Core.Config;
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.Interfaces.Shared;
using BindSharp;

namespace Application.Core.Services.Shared;

/// <summary>
/// Implements deterministic encryption using AES in CBC mode with
/// a key-derived IV, suitable for database lookups and equality comparisons.
/// </summary>
/// <remarks>
/// WARNING: Deterministic encryption is less secure than randomized encryption and
/// should only be used when absolutely necessary, such as for searchable fields.
/// </remarks>
public sealed class DeterministicAesEncryptionService : IDeterministicEncryption, IDisposable
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _ivGenerationKey;
    private bool _disposed;

    private const int BlockSize = 16;
    private const int HmacSize = 32;

    public DeterministicAesEncryptionService(DeterministicEncryptionConfig config)
    {
        var keyResult = ValidateAndLoadKeys(config);

        if (keyResult.IsFailure)
            throw new ArgumentException(keyResult.Error.Message, nameof(config));

        (_encryptionKey, _ivGenerationKey) = keyResult.Value;
    }

    public Result<string, DeterministicEncryptionError> Encrypt(string plaintext) =>
        ValidatePlainText(plaintext)
            .Bind(text => Result.Try(
                () => Encoding.UTF8.GetBytes(text),
                DeterministicEncryptionError (ex) => new GetBytesDeterministicError(ex.Message, ex)
            ))
            .Bind(EncryptBytes)
            .Map(Convert.ToBase64String);

    public Result<string, DeterministicEncryptionError> Decrypt(string ciphertext) =>
        ValidateCipherText(ciphertext)
            .Bind(text => Result.Try(
                () => Convert.FromBase64String(text),
                DeterministicEncryptionError (ex) => new GetBytesFromBase64StringDeterministicError(ex.Message, ex)
            )
            .Bind(DecryptBytes)
            .Bind(bytes => Result.Try(
                () => Encoding.UTF8.GetString(bytes),
                DeterministicEncryptionError (ex) => new GetBytesDeterministicError(ex.Message, ex)
            )));

    private static Result<string, DeterministicEncryptionError> ValidatePlainText(string plaintext) =>
        !string.IsNullOrEmpty(plaintext)
            ? plaintext
            : new EmptyPlainTextDeterministicError();

    private static Result<string, DeterministicEncryptionError> ValidateCipherText(string ciphertext) =>
        !string.IsNullOrEmpty(ciphertext)
            ? ciphertext
            : new EmptyCypherTextDeterministicError();

    private Result<byte[], DeterministicEncryptionError> EncryptBytes(byte[] plainBytes) =>
        ValidatePlainBytes(plainBytes)
            .Map(GenerateDeterministicIv)
            .Bind(iv => PerformAesEncryption(plainBytes, iv)
                .Map(encrypted => (iv, encrypted)))
            .Map(data => AddAuthenticationTag(data.iv, data.encrypted))
            .Map(data => CombineEncryptedParts(data.iv, data.authTag, data.ciphertext));

    private Result<byte[], DeterministicEncryptionError> DecryptBytes(byte[] encryptedBytes) =>
        ValidateEncryptedBytesLength(encryptedBytes)
            .Bind(ExtractEncryptedParts)
            .Bind(VerifyAuthenticationTag)
            .Bind(parts => PerformAesDecryption(parts.iv, parts.ciphertext));

    private static Result<byte[], DeterministicEncryptionError> ValidatePlainBytes(byte[] plainBytes) =>
        plainBytes is { Length: > 0 }
            ? plainBytes
            : new EmptyPlainBytesDeterministicError();

    private static Result<byte[], DeterministicEncryptionError> ValidateEncryptedBytesLength(byte[] encryptedBytes)
    {
        const int minLength = BlockSize + HmacSize;
        return encryptedBytes is { Length: > 0 and > minLength }
            ? encryptedBytes
            : new InsufficientEncryptedBytesLength($"Encrypted data too short. Expected more than {minLength} bytes");
    }

    private byte[] GenerateDeterministicIv(byte[] data)
    {
        using HMACSHA256 hmac = new(_ivGenerationKey);
        byte[] hash = hmac.ComputeHash(data);

        byte[] iv = new byte[BlockSize];
        Buffer.BlockCopy(hash, 0, iv, 0, BlockSize);
        return iv;
    }

    private Result<byte[], DeterministicEncryptionError> PerformAesEncryption(byte[] plainBytes, byte[] iv) =>
        Result.Try(() =>
                {
                    using Aes aes = Aes.Create();
                    aes.Key = _encryptionKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using ICryptoTransform encryptor = aes.CreateEncryptor();
                    return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                },
                DeterministicEncryptionError (ex) => new AesEncryptionError(ex.Message, ex));

    private (byte[] iv, byte[] authTag, byte[] ciphertext) AddAuthenticationTag(byte[] iv, byte[] ciphertext)
    {
        byte[] authTag = ComputeHmac(iv, ciphertext);
        return (iv, authTag, ciphertext);
    }

    private static byte[] CombineEncryptedParts(byte[] iv, byte[] authTag, byte[] ciphertext)
    {
        byte[] result = new byte[iv.Length + authTag.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(authTag, 0, result, iv.Length, authTag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, iv.Length + authTag.Length, ciphertext.Length);
        return result;
    }

    private static Result<(byte[] iv, byte[] authTag, byte[] ciphertext), DeterministicEncryptionError>
        ExtractEncryptedParts(byte[] encryptedBytes) =>
        Result.Try(() =>
            {
                byte[] iv = new byte[BlockSize];
                byte[] authTag = new byte[HmacSize];
                int ciphertextLength = encryptedBytes.Length - BlockSize - HmacSize;
                byte[] ciphertext = new byte[ciphertextLength];

                Buffer.BlockCopy(encryptedBytes, 0, iv, 0, BlockSize);
                Buffer.BlockCopy(encryptedBytes, BlockSize, authTag, 0, HmacSize);
                Buffer.BlockCopy(encryptedBytes, BlockSize + HmacSize, ciphertext, 0, ciphertextLength);

                return (iv, authTag, ciphertext);
                
            },
            DeterministicEncryptionError (ex) => new AesEncryptedPartsExtractionError(ex.Message, ex));

    private Result<(byte[] iv, byte[] ciphertext), DeterministicEncryptionError>
        VerifyAuthenticationTag((byte[] iv, byte[] authTag, byte[] ciphertext) parts)
    {
        byte[] computedAuthTag = ComputeHmac(parts.iv, parts.ciphertext);

        return CryptographicOperations.FixedTimeEquals(parts.authTag, computedAuthTag)
            ? (parts.iv, parts.ciphertext)
            : new InvalidAuthenticationTag();
    }

    private Result<byte[], DeterministicEncryptionError> PerformAesDecryption(byte[] iv, byte[] ciphertext) =>
        Result.Try(() =>
            {
                using Aes aes = Aes.Create();
                aes.Key = _encryptionKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using ICryptoTransform decryptor = aes.CreateDecryptor();
                return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                
            },
            DeterministicEncryptionError (ex) => new AesDecryptionError("Decryption failed. Data might be corrupted or tampered with", ex.Message, ex));

    private byte[] ComputeHmac(byte[] iv, byte[] ciphertext)
    {
        byte[] combined = new byte[iv.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, combined, iv.Length, ciphertext.Length);

        using HMACSHA256 hmac = new(_encryptionKey);
        return hmac.ComputeHash(combined);
    }

    private static Result<(byte[] encryptionKey, byte[] ivGenerationKey), DeterministicEncryptionError>
        ValidateAndLoadKeys(DeterministicEncryptionConfig config) =>
        ValidateKeyStrings(config)
            .Bind(DecodeKeys)
            .Bind(ValidateKeyLengths)
            .Map(CopyKeysToNewArrays);

    private static Result<DeterministicEncryptionConfig, DeterministicEncryptionError>
        ValidateKeyStrings(DeterministicEncryptionConfig config)
    {
        if (string.IsNullOrEmpty(config.MasterKey))
            return new NullOrEmptyEncryptionKeyError();

        if (string.IsNullOrEmpty(config.IvGenerationKey))
            return new NullOrEmptyIvGenerationKeyError();

        return config;
    }

    private static Result<(byte[] encryptionKey, byte[] ivGenerationKey), DeterministicEncryptionError>
        DecodeKeys(DeterministicEncryptionConfig config) =>
        Result.Try(() =>
            {
                byte[] encryptionKey = Convert.FromBase64String(config.MasterKey);
                byte[] ivGenerationKey = Convert.FromBase64String(config.IvGenerationKey);
                return (encryptionKey, ivGenerationKey);
            },
            DeterministicEncryptionError (ex) => new InvalidAesKeysError());

    private static Result<(byte[] encryptionKey, byte[] ivGenerationKey), DeterministicEncryptionError>
        ValidateKeyLengths((byte[] encryptionKey, byte[] ivGenerationKey) keys)
    {
        if (keys.encryptionKey.Length != 32)
            return new InvalidEncryptionKeyConfigurationError($"Encryption key must be 32 bytes (256 bits), got {keys.encryptionKey.Length} bytes");

        return keys.ivGenerationKey.Length != 32 
            ? new InvalidIvGenerationKeyConfigurationError($"IV generation key must be 32 bytes (256 bits), got {keys.ivGenerationKey.Length} bytes")
            : keys;
    }

    private static (byte[] encryptionKey, byte[] ivGenerationKey)
        CopyKeysToNewArrays((byte[] encryptionKey, byte[] ivGenerationKey) keys)
    {
        byte[] encryptionKeyCopy = new byte[keys.encryptionKey.Length];
        byte[] ivGenerationKeyCopy = new byte[keys.ivGenerationKey.Length];

        Buffer.BlockCopy(keys.encryptionKey, 0, encryptionKeyCopy, 0, keys.encryptionKey.Length);
        Buffer.BlockCopy(keys.ivGenerationKey, 0, ivGenerationKeyCopy, 0, keys.ivGenerationKey.Length);

        return (encryptionKeyCopy, ivGenerationKeyCopy);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Array.Clear(_encryptionKey, 0, _encryptionKey.Length);
            Array.Clear(_ivGenerationKey, 0, _ivGenerationKey.Length);
        }
        _disposed = true;
    }

    ~DeterministicAesEncryptionService()
    {
        Dispose(false);
    }
}