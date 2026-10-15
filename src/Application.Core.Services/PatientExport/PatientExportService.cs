using Application.Core.DTOs.Appointment.Errors;
using Application.Core.DTOs.Appointment.Request;
using Application.Core.DTOs.Appointment.Response;
using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Diagnostic.Request;
using Application.Core.DTOs.Diagnostic.Response;
using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Patient.Response;
using Application.Core.DTOs.PatientExport.Response;
using Application.Core.DTOs.Prescription.Errors;
using Application.Core.DTOs.Prescription.Response;
using Application.Core.Interfaces.Appointment;
using Application.Core.Interfaces.Diagnostic;
using Application.Core.Interfaces.Patient;
using Application.Core.Interfaces.PatientExport;
using Application.Core.Interfaces.Prescription;
using BindSharp;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;

namespace Application.Core.Services.PatientExport;

public sealed class PatientExportService : IPatientExport
{
    private const int ExportPageSize = 100;

    private readonly IPatient _patients;
    private readonly IDiagnostic _diagnostics;
    private readonly IPrescription _prescriptions;
    private readonly IAppointment _appointments;
    private readonly IActivityLogger _activityLogger;
    private readonly IUserContext _userContext;

    public PatientExportService(IPatient patients, IDiagnostic diagnostics, IPrescription prescriptions,
        IAppointment appointments, IActivityLogger activityLogger, IUserContext userContext)
    {
        _patients = patients;
        _diagnostics = diagnostics;
        _prescriptions = prescriptions;
        _appointments = appointments;
        _activityLogger = activityLogger;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<PatientExportResponse, PatientError>> ExportAsync(Guid patientCode)
    {
        Result<PatientResponse, PatientError> profile = await _patients.GetSelfAsync(patientCode);
        if (!profile.IsSuccess)
            return Result<PatientExportResponse, PatientError>.Failure(profile.Error);

        Result<List<DiagnosticResponse>, PatientError> diagnostics = await ReadDiagnosticsAsync(patientCode);
        if (!diagnostics.IsSuccess)
            return Result<PatientExportResponse, PatientError>.Failure(diagnostics.Error);

        Result<List<PrescriptionResponse>, PatientError> prescriptions =
            await ReadPrescriptionsAsync(diagnostics.Value);
        if (!prescriptions.IsSuccess)
            return Result<PatientExportResponse, PatientError>.Failure(prescriptions.Error);

        Result<List<AppointmentResponse>, PatientError> appointments = await ReadAppointmentsAsync(patientCode);
        if (!appointments.IsSuccess)
            return Result<PatientExportResponse, PatientError>.Failure(appointments.Error);

        // Counts only: the event never carries clinical content.
        await _activityLogger.LogAsync(new ActivityEvent(
            ActivityEventTypes.PatientExportGenerated, _userContext.UserCode, ActivityEntityTypes.Patient,
            patientCode, "Generated patient record export",
            new
            {
                RequestedByRole = _userContext.Role,
                Diagnostics = diagnostics.Value.Count,
                Prescriptions = prescriptions.Value.Count,
                Appointments = appointments.Value.Count
            }));

        return Result<PatientExportResponse, PatientError>.Success(new PatientExportResponse(
            DateTimeOffset.UtcNow, profile.Value, diagnostics.Value, prescriptions.Value, appointments.Value));
    }

    private async Task<Result<List<DiagnosticResponse>, PatientError>> ReadDiagnosticsAsync(Guid patientCode)
    {
        List<DiagnosticResponse> items = [];
        for (int page = 1;; page++)
        {
            Result<DiagnosticPageResponse, DiagnosticError> result = await _diagnostics.GetPageAsync(
                patientCode, new DiagnosticPageRequest { Page = page, PageSize = ExportPageSize });
            if (!result.IsSuccess)
                return Result<List<DiagnosticResponse>, PatientError>.Failure(ToPatientError(result.Error));

            items.AddRange(result.Value.Items);
            if (page >= result.Value.TotalPages)
                return Result<List<DiagnosticResponse>, PatientError>.Success(items);
        }
    }

    private async Task<Result<List<PrescriptionResponse>, PatientError>> ReadPrescriptionsAsync(
        IEnumerable<DiagnosticResponse> diagnostics)
    {
        List<PrescriptionResponse> items = [];
        foreach (Guid code in diagnostics.Where(d => d.Prescription is not null)
                     .Select(d => d.Prescription!.PrescriptionCode))
        {
            Result<PrescriptionResponse, PrescriptionError> result = await _prescriptions.GetAsync(code);
            if (!result.IsSuccess)
                return Result<List<PrescriptionResponse>, PatientError>.Failure(ToPatientError(result.Error));
            items.Add(result.Value);
        }

        return Result<List<PrescriptionResponse>, PatientError>.Success(items);
    }

    private async Task<Result<List<AppointmentResponse>, PatientError>> ReadAppointmentsAsync(Guid patientCode)
    {
        List<AppointmentResponse> items = [];
        for (int page = 1;; page++)
        {
            Result<AppointmentPageResponse, AppointmentError> result = await _appointments
                .GetPatientAppointmentsAsync(patientCode, new AppointmentPageRequest(page, ExportPageSize));
            if (!result.IsSuccess)
                return Result<List<AppointmentResponse>, PatientError>.Failure(ToPatientError(result.Error));

            items.AddRange(result.Value.Items);
            if (items.Count >= result.Value.Total || result.Value.Items.Count == 0)
                return Result<List<AppointmentResponse>, PatientError>.Success(items);
        }
    }

    private static PatientError ToPatientError(DiagnosticError error) => error is DiagnosticAccessDeniedError
        ? new PatientAccessDeniedError()
        : new PatientDataAccessError(error.Message, error.Details, error.Exception);

    private static PatientError ToPatientError(PrescriptionError error) => error is PrescriptionPatientForbiddenError
        ? new PatientAccessDeniedError()
        : new PatientDataAccessError(error.Message, error.Details, error.Exception);

    private static PatientError ToPatientError(AppointmentError error) => error is AppointmentForbiddenError
        ? new PatientAccessDeniedError()
        : new PatientDataAccessError(error.Message, error.Details, error.Exception);
}
