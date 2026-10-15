namespace Application.Core.DTOs.Disclosure.Errors;

public abstract record DisclosureError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record DisclosureDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : DisclosureError(Message, Details, Exception);

public sealed record DisclosurePatientNotFoundError()
    : DisclosureError("No se encontró un paciente activo.");

public sealed record DisclosureAccessDeniedError()
    : DisclosureError("No tiene acceso a este paciente.");

public sealed record DisclosureInvalidError(string Message) : DisclosureError(Message);
