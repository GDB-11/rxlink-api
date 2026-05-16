namespace Application.Core.DTOs.Auth.Errors;

public abstract record AuthenticationError(string Message, string? Details = null, Exception? Exception = null);

public sealed record StoreRefreshTokenError(string Message, string? Details, Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);
    
public sealed record GetByUsernameAsyncDomainError(string Message, string? Details = null, Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);

public sealed record GetByRefreshTokenAsyncDomainError(string Message, string? Details = null, Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);
    
public sealed record UserNotFoundError()
    : AuthenticationError("No user was found.");
    
public sealed record RefreshTokenNotFoundError()
    : AuthenticationError("No refresh token was found.");
    
public sealed record JwtGenerationError(string? Details = null, Exception? Exception = null)
    : AuthenticationError("Failed to generate refresh token.", Details , Exception);
    
public sealed record UserInactiveError()
    : AuthenticationError("The user is not active.");

public sealed record InvalidUserTokenError()
    : AuthenticationError("The token is not valid.");
    
public sealed record ChaChaDecryptError(string Message, string? Details, Exception? Exception = null)
    : AuthenticationError(Message, Details, Exception);
    
public sealed record JwtStorageError(string? Details, Exception? Exception = null)
    : AuthenticationError("Failed to store refresh token.", Details, Exception);