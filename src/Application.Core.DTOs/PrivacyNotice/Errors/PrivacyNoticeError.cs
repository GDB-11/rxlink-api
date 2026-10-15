namespace Application.Core.DTOs.PrivacyNotice.Errors;

public abstract record PrivacyNoticeError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record PrivacyNoticeDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : PrivacyNoticeError(Message, Details, Exception);

/// <summary>No version of the notice has been published (or none is in force yet).</summary>
public sealed record PrivacyNoticeNotPublishedError()
    : PrivacyNoticeError("Aún no se ha publicado un aviso de privacidad vigente.");

public sealed record PrivacyNoticePatientNotFoundError()
    : PrivacyNoticeError("No se encontró un paciente activo.");

public sealed record PrivacyNoticeAccessDeniedError()
    : PrivacyNoticeError("No tiene acceso a este paciente.");

public sealed record PrivacyNoticeInvalidError(string Message) : PrivacyNoticeError(Message);
