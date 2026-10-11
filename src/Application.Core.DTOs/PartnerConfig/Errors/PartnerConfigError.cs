namespace Application.Core.DTOs.PartnerConfig.Errors;

public abstract record PartnerConfigError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>The registry could not be read, or this deployment has no registry row.</summary>
public sealed record PartnerConfigUnavailableError(string? Details = null, Exception? Exception = null)
    : PartnerConfigError("La configuración del centro no está disponible en este momento. Intente nuevamente más tarde.", Details, Exception);

/// <summary>The partner has no logo uploaded.</summary>
public sealed record PartnerLogoNotFoundError()
    : PartnerConfigError("El centro no tiene un logo configurado.");
