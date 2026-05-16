namespace Application.Core.DTOs.Encryption.Errors;

public abstract record DeterministicEncryptionError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetBytesDeterministicError(string? Details = null, Exception? Exception = null)
    : DeterministicEncryptionError("There was an error getting the bytes from the pain text.", Details, Exception);
    
public sealed record GetBytesFromBase64StringDeterministicError(string? Details = null, Exception? Exception = null)
    : DeterministicEncryptionError("There was an error getting the bytes from the base 64 string.", Details, Exception);
    
public sealed record EmptyPlainTextDeterministicError() 
    : DeterministicEncryptionError("The plain text cannot be null or empty.");
    
public sealed record EmptyCypherTextDeterministicError() 
    : DeterministicEncryptionError("The cypher text cannot be null or empty.");
    
public sealed record EmptyPlainBytesDeterministicError() 
    : DeterministicEncryptionError("The plain bytes cannot be null or empty.");
    
public sealed record InsufficientEncryptedBytesLength(string Message)
    : DeterministicEncryptionError(Message);
    
public sealed record AesEncryptionError(string? Details = null, Exception? Exception = null)
    : DeterministicEncryptionError("Failed to perform the encryption.", Details, Exception);
    
public sealed record AesEncryptedPartsExtractionError(string? Details = null, Exception? Exception = null)
    : DeterministicEncryptionError("Failed to extract encrypted parts.", Details, Exception);
    
public sealed record InvalidAuthenticationTag()
    : DeterministicEncryptionError("Authentication tag validation failed. Data may be corrupted or tampered with.");

public sealed record AesDecryptionError(string Message, string? Details = null, Exception? Exception = null)
    : DeterministicEncryptionError(Message, Details, Exception);
    
public sealed record NullOrEmptyEncryptionKeyError()
    : DeterministicEncryptionError("Encryption key cannot be null or empty");

public sealed record NullOrEmptyIvGenerationKeyError()
    : DeterministicEncryptionError("IV generation cannot be null or empty");
    
public sealed record InvalidAesKeysError()
    : DeterministicEncryptionError("Keys must be valid Base64 strings");
    
public sealed record InvalidEncryptionKeyConfigurationError(string Message)
    : DeterministicEncryptionError(Message);
    
public sealed record InvalidIvGenerationKeyConfigurationError(string Message)
    : DeterministicEncryptionError(Message);