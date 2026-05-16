namespace Application.Core.DTOs.Encryption.Errors;

public abstract record ChaChaEncryptionError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetBytesError(string Details, Exception? Exception = null)
    : ChaChaEncryptionError("There as an error getting the bytes from the pain text.", Details, Exception);

public sealed record GetBytesFromBase64StringError(string Details, Exception? Exception = null)
    : ChaChaEncryptionError("There as an error getting the bytes from the base 64 string.", Details, Exception);

public sealed record ChaChaEncryptError(string Details, Exception? Exception = null)
    : ChaChaEncryptionError("There as an error encrypting using the ChaCha20-Poly1305 algorithm", Details, Exception);

public sealed record ChaChaDecryptError(string Details, Exception? Exception = null)
    : ChaChaEncryptionError("There as an error decrypting using the ChaCha20-Poly1305 algorithm", Details, Exception);

public sealed record PerformDecryption(string Details, Exception? Exception = null)
    : ChaChaEncryptionError("Decryption failed. Data might be corrupted or tampered with", Details, Exception);
    
public sealed record ExtractEncryptedPartsError(string Details, Exception? Exception = null)
    : ChaChaEncryptionError("Failed extracting encrypted parts", Details, Exception);