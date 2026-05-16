namespace Application.Core.DTOs.Encryption.Errors;

public abstract record EncryptionError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GenerateNewKeyPairAsyncError(string? Details = null, Exception? Exception = null)
    : EncryptionError("An unexpected error occurred while generating a key pair.", Details, Exception);
    
public sealed record PersistKeyPairAsyncError(string? Details = null, Exception? Exception = null)
    : EncryptionError("An unexpected error occurred while saving the key pair in the database.", Details, Exception);

public sealed record GetKeyError(string? Details = null, Exception? Exception = null)
    : EncryptionError("There was a problem retrieving the key from the database.", Details, Exception);

public sealed record CreateInstanceError(Exception? Exception = null)
    : EncryptionError("Failed to create instance to decrypt.", string.Empty, Exception);

public sealed record SetPropertyValueError(Exception? Exception = null)
    : EncryptionError("Error creating property value during decryption.", string.Empty, Exception);

public sealed record ImportPrivateKeyError()
    : EncryptionError("Failed to import private key.");

public sealed record KeyNotFoundError()
    : EncryptionError("They key was not found or has expired.");
    
public sealed record KeyAlreadyUsedError() 
    : EncryptionError("The key has already been used.");
    
public sealed record DecryptValueError(Exception? Exception = null)
    : EncryptionError("Unexpected error when decrypting value.", string.Empty, Exception);
    
public sealed record CopyPropertyError(Exception? Exception = null)
    : EncryptionError("Unexpected error when copying property value.", string.Empty, Exception);