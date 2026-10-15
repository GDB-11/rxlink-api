namespace Infrastructure.Core.DTOs.PartnerConfig;

public abstract record PartnerConfigRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetPartnerCodeError(string? Details = null, Exception? Exception = null)
    : PartnerConfigRepositoryError("Error inesperado al recuperar la configuración del partner.", Details, Exception);

public sealed record HeartbeatError(string? Details = null, Exception? Exception = null)
    : PartnerConfigRepositoryError("No se pudo enviar el latido al catálogo.", Details, Exception);
