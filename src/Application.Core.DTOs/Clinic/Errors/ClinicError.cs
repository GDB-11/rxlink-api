namespace Application.Core.DTOs.Clinic.Errors;

public abstract record ClinicError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record ClinicDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : ClinicError(Message, Details, Exception);

/// <summary>The deployment has no active default clinic row.</summary>
public sealed record ClinicNotFoundError()
    : ClinicError("No se encontró el centro de atención configurado.");
