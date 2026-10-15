namespace Infrastructure.Core.DTOs.Amendment;

public abstract record AmendmentRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record InsertAmendmentError(string? Details = null, Exception? Exception = null)
    : AmendmentRepositoryError("Error inesperado al registrar la solicitud de enmienda.", Details, Exception);

public sealed record GetAmendmentError(string? Details = null, Exception? Exception = null)
    : AmendmentRepositoryError("Error inesperado al recuperar las solicitudes de enmienda.", Details, Exception);

public sealed record DecideAmendmentError(string? Details = null, Exception? Exception = null)
    : AmendmentRepositoryError("Error inesperado al resolver la solicitud de enmienda.", Details, Exception);
