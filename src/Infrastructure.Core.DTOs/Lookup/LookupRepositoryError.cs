namespace Infrastructure.Core.DTOs.Lookup;

public abstract record LookupRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetLookupError(string? Details = null, Exception? Exception = null)
    : LookupRepositoryError("Error inesperado al recuperar los datos de catálogo.", Details, Exception);
