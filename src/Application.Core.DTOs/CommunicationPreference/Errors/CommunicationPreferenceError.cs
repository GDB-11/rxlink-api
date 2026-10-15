namespace Application.Core.DTOs.CommunicationPreference.Errors;

public abstract record CommunicationPreferenceError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record CommunicationPreferenceDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : CommunicationPreferenceError(Message, Details, Exception);

public sealed record CommunicationPreferencePatientNotFoundError()
    : CommunicationPreferenceError("No se encontró un paciente activo.");

public sealed record CommunicationPreferenceAccessDeniedError()
    : CommunicationPreferenceError("No tiene acceso a este paciente.");

public sealed record CommunicationPreferenceInvalidError(string Message) : CommunicationPreferenceError(Message);
