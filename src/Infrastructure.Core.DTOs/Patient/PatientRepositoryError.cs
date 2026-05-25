namespace Infrastructure.Core.DTOs.Patient;

public abstract record PatientRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetPatientsPageError(string? Details = null, Exception? Exception = null)
    : PatientRepositoryError("Error inesperado al recuperar los pacientes.", Details, Exception);

public sealed record InsertPatientError(string? Details = null, Exception? Exception = null)
    : PatientRepositoryError("Error inesperado al registrar el paciente.", Details, Exception);

public sealed record UpdatePatientError(string? Details = null, Exception? Exception = null)
    : PatientRepositoryError("Error inesperado al actualizar el paciente.", Details, Exception);

public sealed record DeactivatePatientError(string? Details = null, Exception? Exception = null)
    : PatientRepositoryError("Error inesperado al desactivar el paciente.", Details, Exception);
