using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.PatientExport.Response;
using BindSharp;

namespace Application.Core.Interfaces.PatientExport;

public interface IPatientExport
{
    /// <summary>
    /// Builds the full export of a patient's record for the current caller (the patient themselves or staff
    /// acting for them) and records a <c>Patient.ExportGenerated</c> event. Denied entirely when any part
    /// is out of the caller's scope, so an export is never silently partial.
    /// </summary>
    Task<Result<PatientExportResponse, PatientError>> ExportAsync(Guid patientCode);
}
