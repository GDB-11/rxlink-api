namespace Infrastructure.Core.DTOs.CommunicationPreference;

public abstract record CommunicationPreferenceRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetCommunicationPreferenceError(string? Details = null, Exception? Exception = null)
    : CommunicationPreferenceRepositoryError("Error inesperado al recuperar las preferencias de comunicación.", Details, Exception);

public sealed record SaveCommunicationPreferenceError(string? Details = null, Exception? Exception = null)
    : CommunicationPreferenceRepositoryError("Error inesperado al guardar las preferencias de comunicación.", Details, Exception);
