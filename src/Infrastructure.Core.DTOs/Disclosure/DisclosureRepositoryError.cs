namespace Infrastructure.Core.DTOs.Disclosure;

public abstract record DisclosureRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record InsertDisclosureError(string? Details = null, Exception? Exception = null)
    : DisclosureRepositoryError("Error inesperado al registrar la divulgación.", Details, Exception);

public sealed record GetDisclosureError(string? Details = null, Exception? Exception = null)
    : DisclosureRepositoryError("Error inesperado al recuperar las divulgaciones.", Details, Exception);
