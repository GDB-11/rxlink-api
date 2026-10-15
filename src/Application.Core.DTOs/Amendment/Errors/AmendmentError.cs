namespace Application.Core.DTOs.Amendment.Errors;

public abstract record AmendmentError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record AmendmentDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : AmendmentError(Message, Details, Exception);

public sealed record AmendmentNotFoundError()
    : AmendmentError("No se encontró la solicitud de enmienda.");

public sealed record AmendmentPatientNotFoundError()
    : AmendmentError("No se encontró un paciente activo.");

public sealed record AmendmentAccessDeniedError()
    : AmendmentError("No tiene acceso a este paciente.");

/// <summary>Unknown area or status in the request.</summary>
public sealed record AmendmentInvalidError(string Message) : AmendmentError(Message);

/// <summary>The request was already accepted or denied; a decision is final.</summary>
public sealed record AmendmentAlreadyDecidedError()
    : AmendmentError("La solicitud ya fue resuelta y no puede modificarse.");
