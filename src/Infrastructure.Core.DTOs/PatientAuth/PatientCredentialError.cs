namespace Infrastructure.Core.DTOs.PatientAuth;

public abstract record PatientCredentialError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetByDocumentAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);

public sealed record GetByPersonCodeAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);

public sealed record GetByEmailAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);

public sealed record GetByRefreshTokenAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);

public sealed record CreatePersonAndPatientAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);

public sealed record CreatePatientForPersonAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);

public sealed record AddCredentialsAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);

public sealed record UpdateRefreshTokenAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);

public sealed record ClearRefreshTokenAsyncError(string Message, Exception? Exception = null)
    : PatientCredentialError(Message, null, Exception);