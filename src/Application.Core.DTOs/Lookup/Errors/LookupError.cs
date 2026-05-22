namespace Application.Core.DTOs.Lookup.Errors;

public abstract record LookupError(string Message, string? Details = null, Exception? Exception = null);

public sealed record LookupDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : LookupError(Message, Details, Exception);
