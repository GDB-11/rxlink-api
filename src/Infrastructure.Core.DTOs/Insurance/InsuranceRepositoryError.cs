namespace Infrastructure.Core.DTOs.Insurance;

public abstract record InsuranceRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetInsurancePageError(string? Details = null, Exception? Exception = null)
    : InsuranceRepositoryError("Error inesperado al recuperar los seguros.", Details, Exception);

public sealed record InsertInsuranceError(string? Details = null, Exception? Exception = null)
    : InsuranceRepositoryError("Error inesperado al registrar el seguro.", Details, Exception);

public sealed record UpdateInsuranceError(string? Details = null, Exception? Exception = null)
    : InsuranceRepositoryError("Error inesperado al actualizar el seguro.", Details, Exception);

public sealed record DeactivateInsuranceError(string? Details = null, Exception? Exception = null)
    : InsuranceRepositoryError("Error inesperado al desactivar el seguro.", Details, Exception);
