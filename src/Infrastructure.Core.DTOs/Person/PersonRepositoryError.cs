namespace Infrastructure.Core.DTOs.Person;

public abstract record PersonRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetPersonsPageError(string? Details = null, Exception? Exception = null)
    : PersonRepositoryError("Error inesperado al recuperar las personas.", Details, Exception);

public sealed record InsertPersonError(string? Details = null, Exception? Exception = null)
    : PersonRepositoryError("Error inesperado al registrar la persona.", Details, Exception);

public sealed record UpdatePersonError(string? Details = null, Exception? Exception = null)
    : PersonRepositoryError("Error inesperado al actualizar la persona.", Details, Exception);