namespace Infrastructure.Core.DTOs.Prescription;

public abstract record PrescriptionRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetPrescriptionError(string? Details = null, Exception? Exception = null)
    : PrescriptionRepositoryError("Error inesperado al recuperar la receta.", Details, Exception);

public sealed record InsertPrescriptionError(string? Details = null, Exception? Exception = null)
    : PrescriptionRepositoryError("Error inesperado al registrar la receta.", Details, Exception);

public sealed record InsertPrescriptionDuplicateError(string? Details = null, Exception? Exception = null)
    : PrescriptionRepositoryError("Ya existe una receta activa para este diagnóstico.", Details, Exception);

public sealed record UpdatePrescriptionError(string? Details = null, Exception? Exception = null)
    : PrescriptionRepositoryError("Error inesperado al actualizar la receta.", Details, Exception);

public sealed record UpdatePrescriptionInvalidStatusError(string? Details = null, Exception? Exception = null)
    : PrescriptionRepositoryError("La receta no se puede modificar porque no está en estado Borrador.", Details,
        Exception);

public sealed record ChangeStatusPrescriptionError(string? Details = null, Exception? Exception = null)
    : PrescriptionRepositoryError("Error inesperado al cambiar el estado de la receta.", Details, Exception);