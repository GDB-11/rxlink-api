using Application.Core.DTOs.Appointment.Errors;
using Application.Core.DTOs.Appointment.Request;
using Application.Core.DTOs.Appointment.Response;
using Application.Core.Interfaces.Appointment;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.DTOs.Appointment;
using Infrastructure.Core.Interfaces.Appointment;
using Infrastructure.Core.Models.Appointment;

namespace Application.Core.Services.Appointment;

public sealed class AppointmentService : IAppointment
{
    private readonly IAppointmentRepository _repository;

    public AppointmentService(IAppointmentRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<AppointmentResponse, AppointmentError>> CreateAsync(
        CreateAppointmentRequest request, Guid patientCode) =>
        _repository.InsertAsync(
                patientCode,
                request.AvailabilityCode,
                request.ConsultationTypeCode)
            .MapErrorAsync(AppointmentError (error) => error switch
            {
                InsertPatientNotFoundError => new AppointmentPatientNotFoundError(),
                InsertSlotNotFoundError => new AppointmentSlotNotFoundError(),
                InsertSlotAlreadyBookedError => new AppointmentSlotAlreadyBookedError(),
                InsertSlotExpiredError => new AppointmentSlotExpiredError(),
                InsertConsultationTypeNotFoundError => new AppointmentConsultationTypeNotFoundError(),
                var e => new AppointmentDataAccessError(e.Message, e.Details, e.Exception)
            })
            .EnsureNotNullAsync(new AppointmentSlotAlreadyBookedError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, AppointmentError>> ConfirmPaymentAsync(Guid code, Guid patientCode) =>
        _repository.ConfirmPaymentAsync(code, patientCode)
            .MapErrorAsync(AppointmentError (error) =>
                new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new AppointmentInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, AppointmentError>> CancelAsync(
        Guid code, Guid callerCode, string callerRole)
    {
        Guid? patientCode = callerRole == "Patient" ? callerCode : (Guid?)null;

        return _repository.CancelAsync(code, patientCode)
            .MapErrorAsync(AppointmentError (error) =>
                new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new AppointmentInvalidTransitionError())
            .MapAsync(_ => Unit.Value);
    }

    /// <inheritdoc/>
    public Task<Result<Unit, AppointmentError>> CompleteAsync(
        Guid code, Guid callerUserCode, string callerRole) =>
        Result<Unit, AppointmentError>.Success(Unit.Value)
            .AsTask()
            .BindIfAsync(
                _ => callerRole == "Doctor",
                _ => _repository.GetByCodeAsync(code)
                    .MapErrorAsync(AppointmentError (error) =>
                        new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
                    .EnsureNotNullAsync(new AppointmentNotFoundError())
                    .EnsureAsync(row => row.DoctorCode == callerUserCode, new AppointmentForbiddenError())
                    .MapAsync(_ => Unit.Value))
            .BindAsync(_ => _repository.CompleteAsync(code, callerUserCode)
                .MapErrorAsync(AppointmentError (error) =>
                    new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
                .EnsureAsync(affected => affected > 0, new AppointmentInvalidTransitionError())
                .MapAsync(_ => Unit.Value));

    /// <inheritdoc/>
    public Task<Result<Unit, AppointmentError>> NoShowAsync(Guid code, Guid adminUserCode) =>
        _repository.NoShowAsync(code, adminUserCode)
            .MapErrorAsync(AppointmentError (error) =>
                new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new AppointmentInvalidTransitionError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<AppointmentResponse, AppointmentError>> GetAsync(
        Guid code, Guid callerCode, string callerRole) =>
        _repository.GetByCodeAsync(code)
            .MapErrorAsync(AppointmentError (error) =>
                new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new AppointmentNotFoundError())
            .EnsureAsync(row => callerRole switch
            {
                "Patient" => row.PatientCode == callerCode,
                "Doctor" => row.DoctorCode == callerCode,
                "Administrador" => true,
                _ => false
            }, new AppointmentForbiddenError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<AppointmentPageResponse, AppointmentError>> GetPatientAppointmentsAsync(
        Guid patientCode, AppointmentPageRequest request) =>
        _repository.GetPatientAppointmentsAsync(patientCode, request.Page, request.PageSize)
            .MapErrorAsync(AppointmentError (error) =>
                new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(page => new AppointmentPageResponse(
                Items: page.Items.Select(MapToResponse).ToList(),
                Total: page.Total,
                Page: request.Page,
                PageSize: request.PageSize));

    private static AppointmentResponse MapToResponse(AppointmentRow row) =>
        new(
            AppointmentCode: row.AppointmentCode,
            PatientCode: row.PatientCode,
            DoctorCode: row.DoctorCode,
            DoctorNames: row.DoctorNames,
            DoctorSurnames: row.DoctorSurnames,
            SpecialtyName: row.SpecialtyName,
            ConsultationTypeName: row.ConsultationTypeName,
            StatusName: row.StatusName,
            StatusCode: row.StatusCode,
            ScheduledAt: row.ScheduledAt,
            Date: row.ScheduledAt.ToString("yyyy-MM-dd"),
            Time: row.ScheduledAt.ToString("h:mm tt").ToLowerInvariant(),
            CreatedAt: row.CreatedAt);
}