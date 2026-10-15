namespace Application.Core.DTOs.Consent.Errors;

public abstract record ConsentError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record ConsentDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : ConsentError(Message, Details, Exception);

public sealed record ConsentPatientNotFoundError()
    : ConsentError("No se encontró un paciente activo.");

/// <summary>The consent does not exist for that patient, or it was already revoked.</summary>
public sealed record ConsentNotFoundError()
    : ConsentError("No se encontró un consentimiento vigente para revocar.");

public sealed record ConsentAccessDeniedError()
    : ConsentError("No tiene acceso a este paciente.");

public sealed record ConsentInvalidError(string Message) : ConsentError(Message);
