namespace Application.Core.DTOs.Navigation.Errors;

public abstract record NavigationError(string Message, string? Details = null, Exception? Exception = null);
 
/// <summary>Wraps an infrastructure-level repository failure.</summary>
public sealed record NavigationDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : NavigationError(Message, Details, Exception);
 
/// <summary>The JWT did not carry a recognisable role claim.</summary>
public sealed record InvalidRoleClaimError()
    : NavigationError("El token no contiene un rol válido.");