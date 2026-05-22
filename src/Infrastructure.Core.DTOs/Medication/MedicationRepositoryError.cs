namespace Infrastructure.Core.DTOs.Medication;

public abstract record MedicationRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetMedicationsPageError(string? Details = null, Exception? Exception = null)
    : MedicationRepositoryError("Error inesperado al recuperar los medicamentos.", Details, Exception);

public sealed record InsertMedicationError(string? Details = null, Exception? Exception = null)
    : MedicationRepositoryError("Error inesperado al registrar el medicamento.", Details, Exception);

public sealed record UpdateMedicationError(string? Details = null, Exception? Exception = null)
    : MedicationRepositoryError("Error inesperado al actualizar el medicamento.", Details, Exception);

public sealed record DeactivateMedicationError(string? Details = null, Exception? Exception = null)
    : MedicationRepositoryError("Error inesperado al desactivar el medicamento.", Details, Exception);
