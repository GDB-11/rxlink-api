using Application.Core.Interfaces.Catalog;
using Application.Core.DTOs.Appointment.Errors;
using Application.Core.DTOs.Appointment.Request;
using Application.Core.DTOs.Appointment.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.Interfaces.Appointment;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.PatientAccess;
using Infrastructure.Core.Interfaces.Activity;
using BindSharp;
using BindSharp.Extensions;
using Common.Helpers;
using Infrastructure.Core.DTOs.Appointment;
using Infrastructure.Core.Interfaces.Appointment;
using Infrastructure.Core.Models.Appointment;

namespace Application.Core.Services.Appointment;

public sealed class AppointmentService : IAppointment
{
    private readonly IAppointmentRepository _repository;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IPatientAccessPolicy _accessPolicy;
    private readonly IInsuranceTermsResolver _insurance;
    private readonly ICatalogNames _names;

    public AppointmentService(IAppointmentRepository repository, IRecordAccessLogger accessLogger,
        IPatientAccessPolicy accessPolicy, IInsuranceTermsResolver insurance, ICatalogNames names)
    {
        _repository = repository;
        _accessLogger = accessLogger;
        _accessPolicy = accessPolicy;
        _insurance = insurance;
        _names = names;
    }

    private Task<Result<InsuranceTerms?, AppointmentError>> ResolveInsuranceAsync(Guid? insuranceCode) =>
        _insurance.ResolveAsync(insuranceCode)
            .MapErrorAsync(AppointmentError (error) => new AppointmentCatalogValidationError(error));

    private static AppointmentError ToDataError(Application.Core.DTOs.PatientAccess.Errors.PatientAccessError error) =>
        new AppointmentDataAccessError(error.Message, error.Details, error.Exception);

    /// <inheritdoc/>
    public Task<Result<AppointmentResponse, AppointmentError>> CreateAsync(
        CreateAppointmentRequest request, Guid patientCode) =>
        ResolveInsuranceAsync(request.InsuranceCode)
            .BindAsync(insurance => _repository.InsertAsync(
                patientCode,
                request.AvailabilityCode,
                request.ConsultationTypeCode,
                request.PayNow,
                insurance)
            .MapErrorAsync(AppointmentError (error) => error switch
            {
                InsertPatientNotFoundError => new AppointmentPatientNotFoundError(),
                InsertSlotNotFoundError => new AppointmentSlotNotFoundError(),
                InsertSlotAlreadyBookedError => new AppointmentSlotAlreadyBookedError(),
                InsertSlotExpiredError => new AppointmentSlotExpiredError(),
                InsertConsultationTypeNotFoundError => new AppointmentConsultationTypeNotFoundError(),
                InsertSpecialtyPriceNotFoundError => new AppointmentSpecialtyPriceNotFoundError(),
                var e => new AppointmentDataAccessError(e.Message, e.Details, e.Exception)
            })
            .EnsureNotNullAsync(new AppointmentSlotAlreadyBookedError())
            .MapAsync(MapToResponse));

    /// <inheritdoc/>
    public Task<Result<Unit, AppointmentError>> ConfirmPaymentAsync(Guid code, Guid patientCode, Guid? insuranceCode) =>
        ResolveInsuranceAsync(insuranceCode)
            .BindAsync(insurance => _repository.ConfirmPaymentAsync(code, patientCode, insurance)
            .MapErrorAsync(AppointmentError (error) => error switch
            {
                InsertSpecialtyPriceNotFoundError => new AppointmentSpecialtyPriceNotFoundError(),
                var e => new AppointmentDataAccessError(e.Message, e.Details, e.Exception)
            })
            .EnsureAsync(affected => affected > 0, new AppointmentInvalidTransitionError())
            .MapAsync(_ => Unit.Value));

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
    public Task<Result<Unit, AppointmentError>> NoShowAsync(Guid code, Guid callerUserCode, string callerRole) =>
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
            .BindAsync(_ => _repository.NoShowAsync(code, callerUserCode)
                .MapErrorAsync(AppointmentError (error) =>
                    new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
                .EnsureAsync(affected => affected > 0, new AppointmentInvalidTransitionError())
                .MapAsync(_ => Unit.Value));

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
            .BindAsync(row => _accessPolicy
                .RequireAsync<AppointmentError>(row.PatientCode, PatientDataScope.Administrative, ToDataError,
                    new AppointmentForbiddenError())
                .TapAsync(decision => _accessLogger.LogAsync(AccessResourceTypes.Appointment, row.PatientCode,
                    code, decision == PatientAccessDecision.AllowedViaBreakGlass))
                .MapAsync(_ => row))
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<AppointmentPageResponse, AppointmentError>> GetPatientAppointmentsAsync(
        Guid patientCode, AppointmentPageRequest request) =>
        _accessPolicy
            .RequireAsync<AppointmentError>(patientCode, PatientDataScope.Administrative, ToDataError,
                new AppointmentForbiddenError())
            .BindAsync(decision => _repository.GetPatientAppointmentsAsync(patientCode, request.Page, request.PageSize)
                .MapErrorAsync(AppointmentError (error) =>
                    new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
                .TapAsync(_ => _accessLogger.LogAsync(AccessResourceTypes.Appointments, patientCode,
                    viaBreakGlass: decision == PatientAccessDecision.AllowedViaBreakGlass)))
            .MapAsync(page => new AppointmentPageResponse(
                Items: page.Items.Select(MapToResponse).ToList(),
                Total: page.Total,
                Page: request.Page,
                PageSize: request.PageSize));

    /// <inheritdoc/>
    public Task<Result<AppointmentPageResponse, AppointmentError>> GetDoctorAppointmentsAsync(
        Guid doctorUserCode, DoctorAppointmentPageRequest request) =>
        _repository.GetDoctorAppointmentsAsync(
                doctorUserCode, request.Page, request.PageSize, request.Date.ToDateTime(), request.StatusName)
            .MapErrorAsync(AppointmentError (error) =>
                new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(page => new AppointmentPageResponse(
                Items: page.Items.Select(MapToResponse).ToList(),
                Total: page.Total,
                Page: request.Page,
                PageSize: request.PageSize));

    /// <inheritdoc/>
    public Task<Result<AppointmentResponse, AppointmentError>> AdminCreateAsync(
        AdminCreateAppointmentRequest request, Guid adminUserCode) =>
        ResolveInsuranceAsync(request.InsuranceCode)
            .BindAsync(insurance => _repository.InsertByAdminAsync(
                request.PatientCode,
                request.AvailabilityCode,
                request.ConsultationTypeCode,
                request.PayNow,
                insurance,
                adminUserCode)
            .MapErrorAsync(AppointmentError (error) => error switch
            {
                InsertPatientNotFoundError => new AppointmentPatientNotFoundError(),
                InsertSlotNotFoundError => new AppointmentSlotNotFoundError(),
                InsertSlotAlreadyBookedError => new AppointmentSlotAlreadyBookedError(),
                InsertSlotExpiredError => new AppointmentSlotExpiredError(),
                InsertConsultationTypeNotFoundError => new AppointmentConsultationTypeNotFoundError(),
                InsertSpecialtyPriceNotFoundError => new AppointmentSpecialtyPriceNotFoundError(),
                var e => new AppointmentDataAccessError(e.Message, e.Details, e.Exception)
            })
            .EnsureNotNullAsync(new AppointmentSlotAlreadyBookedError())
            .MapAsync(MapToResponse));

    /// <inheritdoc/>
    public Task<Result<Unit, AppointmentError>> AdminConfirmPaymentAsync(
        Guid code, Guid adminUserCode, Guid? insuranceCode) =>
        ResolveInsuranceAsync(insuranceCode)
            .BindAsync(insurance => _repository.ConfirmPaymentByAdminAsync(code, insurance, adminUserCode)
            .MapErrorAsync(AppointmentError (error) => error switch
            {
                InsertSpecialtyPriceNotFoundError => new AppointmentSpecialtyPriceNotFoundError(),
                var e => new AppointmentDataAccessError(e.Message, e.Details, e.Exception)
            })
            .EnsureAsync(affected => affected > 0, new AdminConfirmPaymentConflictError())
            .MapAsync(_ => Unit.Value));

    /// <inheritdoc/>
    public Task<Result<Unit, AppointmentError>> AdminRevertPaymentAsync(Guid code) =>
        _repository.RevertPaymentAsync(code)
            .MapErrorAsync(AppointmentError (error) =>
                new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new RevertPaymentConflictError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<AppointmentPageResponse, AppointmentError>> GetAdminAppointmentsAsync(
        AdminAppointmentPageRequest request) =>
        _repository.GetAdminAppointmentsAsync(
                request.Page, request.PageSize,
                request.PatientSearch, request.Date.ToDateTime(), request.StatusName)
            .MapErrorAsync(AppointmentError (error) =>
                new AppointmentDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(page => new AppointmentPageResponse(
                Items: page.Items.Select(MapToResponse).ToList(),
                Total: page.Total,
                Page: request.Page,
                PageSize: request.PageSize));

    private AppointmentResponse MapToResponse(AppointmentRow row) =>
        new(
            AppointmentCode: row.AppointmentCode,
            PatientCode: row.PatientCode,
            PatientNames: row.PatientNames,
            PatientSurnames: row.PatientSurnames,
            DoctorCode: row.DoctorCode,
            DoctorNames: row.DoctorNames,
            DoctorSurnames: row.DoctorSurnames,
            SpecialtyName: row.SpecialtyCode is { } specialty ? _names.Specialty(specialty) : string.Empty,
            ConsultationTypeName: row.ConsultationTypeName,
            StatusName: row.StatusName,
            StatusCode: row.StatusCode,
            ScheduledAt: row.ScheduledAt,
            Date: row.ScheduledAt.ToString("yyyy-MM-dd"),
            Time: row.ScheduledAt.ToString("h:mm tt").ToLowerInvariant(),
            CreatedAt: row.CreatedAt,
            InsuranceName: row.InsuranceName,
            CoveragePercentage: row.CoveragePercentage,
            BaseAmount: row.BaseAmount,
            PatientAmount: row.PatientAmount);
}