using Application.Core.DTOs.Appointment.Errors;
using Application.Core.DTOs.Appointment.Response;
using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Diagnostic.Request;
using Application.Core.DTOs.Diagnostic.Response;
using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Patient.Response;
using Application.Core.DTOs.Prescription.Response;
using Application.Core.Interfaces.Appointment;
using Application.Core.Interfaces.Diagnostic;
using Application.Core.Interfaces.Patient;
using Application.Core.Interfaces.Prescription;
using Application.Core.Services.PatientExport;
using BindSharp;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using NSubstitute;

namespace Application.Core.Services.Tests.PatientExport;

public sealed class PatientExportServiceTests
{
    private readonly IPatient _patients = Substitute.For<IPatient>();
    private readonly IDiagnostic _diagnostics = Substitute.For<IDiagnostic>();
    private readonly IPrescription _prescriptions = Substitute.For<IPrescription>();
    private readonly IAppointment _appointments = Substitute.For<IAppointment>();
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();
    private readonly IUserContext _user = Substitute.For<IUserContext>();
    private readonly PatientExportService _sut;
    private readonly Guid _patientCode = Guid.NewGuid();

    public PatientExportServiceTests()
    {
        _sut = new PatientExportService(_patients, _diagnostics, _prescriptions, _appointments, _activity, _user);
        _patients.GetSelfAsync(_patientCode).Returns(Result<PatientResponse, PatientError>.Success(new PatientResponse
        {
            PatientCode = _patientCode, PersonCode = Guid.NewGuid(), MedicalRecordNumber = "MRN", IsActive = true,
            Names = "A", Surnames = "B", BirthDate = new DateOnly(1990, 1, 1), Phone = "1", Email = "a@b.c"
        }));
        _appointments.GetPatientAppointmentsAsync(_patientCode, Arg.Any<Application.Core.DTOs.Appointment.Request.AppointmentPageRequest>())
            .Returns(Result<AppointmentPageResponse, AppointmentError>.Success(
                new AppointmentPageResponse([], 0, 1, 100)));
    }

    [Fact]
    public async Task Export_collects_every_diagnostic_page_and_logs_counts_only()
    {
        _diagnostics.GetPageAsync(_patientCode, Arg.Any<DiagnosticPageRequest>()).Returns(
            Result<DiagnosticPageResponse, DiagnosticError>.Success(Page(1, 2, 2)),
            Result<DiagnosticPageResponse, DiagnosticError>.Success(Page(2, 2, 2)));

        var result = await _sut.ExportAsync(_patientCode);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Diagnostics.Count);
        Assert.Empty(result.Value.Prescriptions);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.PatientExportGenerated && e.EntityCode == _patientCode));
    }

    [Fact]
    public async Task Export_is_denied_whole_when_clinical_data_is_out_of_scope()
    {
        _diagnostics.GetPageAsync(_patientCode, Arg.Any<DiagnosticPageRequest>())
            .Returns(Result<DiagnosticPageResponse, DiagnosticError>.Failure(new DiagnosticAccessDeniedError()));

        var result = await _sut.ExportAsync(_patientCode);

        Assert.False(result.IsSuccess);
        Assert.IsType<PatientAccessDeniedError>(result.Error);
        await _activity.DidNotReceive().LogAsync(Arg.Any<ActivityEvent>());
    }

    private DiagnosticPageResponse Page(int page, int totalPages, int total) => new()
    {
        Items = [new DiagnosticResponse
        {
            DiagnosticCode = Guid.NewGuid(), AppointmentCode = Guid.NewGuid(), PatientCode = _patientCode,
            StatusCode = Guid.NewGuid(), StatusName = "Activo", Description = "d", DiagnosedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTimeOffset.UtcNow
        }],
        TotalCount = total, Page = page, PageSize = 1, TotalPages = totalPages
    };
}
