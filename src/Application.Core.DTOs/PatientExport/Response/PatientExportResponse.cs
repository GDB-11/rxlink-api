using Application.Core.DTOs.Appointment.Response;
using Application.Core.DTOs.Diagnostic.Response;
using Application.Core.DTOs.Patient.Response;
using Application.Core.DTOs.Prescription.Response;

namespace Application.Core.DTOs.PatientExport.Response;

/// <summary>
/// Copy of a patient's record for the right of access (HIPAA 164.524). Built only from data the caller
/// is already allowed to read; every part goes through the same access policy and access log as the screens.
/// </summary>
public sealed record PatientExportResponse(
    DateTimeOffset GeneratedAt,
    PatientResponse Profile,
    IReadOnlyList<DiagnosticResponse> Diagnostics,
    IReadOnlyList<PrescriptionResponse> Prescriptions,
    IReadOnlyList<AppointmentResponse> Appointments);
