using Application.Core.DTOs.Appointment.Errors;
using Application.Core.DTOs.Appointment.Request;
using Application.Core.Services.Appointment;
using BindSharp;
using Infrastructure.Core.DTOs.Appointment;
using Infrastructure.Core.Interfaces.Appointment;
using Infrastructure.Core.Models.Appointment;
using NSubstitute;

namespace Application.Core.Services.Tests.Appointment;

public sealed class AppointmentServiceTests
{
    private readonly IAppointmentRepository _repository = Substitute.For<IAppointmentRepository>();
    private readonly AppointmentService _sut;

    public AppointmentServiceTests() => _sut = new AppointmentService(_repository);

    private static AppointmentRow MakeRow(Guid? appointmentCode = null, Guid? patientCode = null,
        Guid? doctorCode = null) => new()
    {
        AppointmentCode = appointmentCode ?? Guid.NewGuid(),
        PatientCode = patientCode ?? Guid.NewGuid(),
        PatientNames = "María",
        PatientSurnames = "García",
        DoctorCode = doctorCode ?? Guid.NewGuid(),
        DoctorNames = "Carlos",
        DoctorSurnames = "López",
        SpecialtyName = "Medicina General",
        ConsultationTypeName = "Consulta",
        StatusName = "Confirmado",
        StatusCode = Guid.NewGuid(),
        ScheduledAt = DateTimeOffset.UtcNow.AddDays(1),
        CreatedAt = DateTimeOffset.UtcNow,
        TotalCount = 1
    };

    // ── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_RepositoryReturnsRow_MapsToAppointmentResponse()
    {
        var row = MakeRow();
        var availabilityCode = Guid.NewGuid();
        var consultationTypeCode = Guid.NewGuid();
        var patientCode = Guid.NewGuid();
        _repository
            .InsertAsync(patientCode, availabilityCode, consultationTypeCode, false, null)
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.CreateAsync(
            new CreateAppointmentRequest(availabilityCode, consultationTypeCode), patientCode);

        Assert.True(result.IsSuccess);
        Assert.Equal(row.AppointmentCode, result.Value.AppointmentCode);
        Assert.Equal(row.PatientCode, result.Value.PatientCode);
    }

    [Fact]
    public async Task CreateAsync_RepositoryReturnsNull_ReturnsSlotAlreadyBookedError()
    {
        _repository
            .InsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(null)));

        var result = await _sut.CreateAsync(
            new CreateAppointmentRequest(Guid.NewGuid(), Guid.NewGuid()), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentSlotAlreadyBookedError>(result.Error);
    }

    [Fact]
    public async Task CreateAsync_PatientNotFoundError_MapsToAppointmentPatientNotFoundError()
    {
        _repository
            .InsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertPatientNotFoundError())));

        var result = await _sut.CreateAsync(
            new CreateAppointmentRequest(Guid.NewGuid(), Guid.NewGuid()), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentPatientNotFoundError>(result.Error);
    }

    [Fact]
    public async Task CreateAsync_SlotNotFoundError_MapsToAppointmentSlotNotFoundError()
    {
        _repository
            .InsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotNotFoundError())));

        var result = await _sut.CreateAsync(
            new CreateAppointmentRequest(Guid.NewGuid(), Guid.NewGuid()), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentSlotNotFoundError>(result.Error);
    }

    [Fact]
    public async Task CreateAsync_SlotAlreadyBookedError_MapsToAppointmentSlotAlreadyBookedError()
    {
        _repository
            .InsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotAlreadyBookedError())));

        var result = await _sut.CreateAsync(
            new CreateAppointmentRequest(Guid.NewGuid(), Guid.NewGuid()), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentSlotAlreadyBookedError>(result.Error);
    }

    [Fact]
    public async Task CreateAsync_SlotExpiredError_MapsToAppointmentSlotExpiredError()
    {
        _repository
            .InsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotExpiredError())));

        var result = await _sut.CreateAsync(
            new CreateAppointmentRequest(Guid.NewGuid(), Guid.NewGuid()), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentSlotExpiredError>(result.Error);
    }

    [Fact]
    public async Task CreateAsync_GenericRepositoryError_MapsToAppointmentDataAccessError()
    {
        _repository
            .InsertAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertAppointmentError("DB failure"))));

        var result = await _sut.CreateAsync(
            new CreateAppointmentRequest(Guid.NewGuid(), Guid.NewGuid()), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentDataAccessError>(result.Error);
    }

    // ── ConfirmPaymentAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmPaymentAsync_RepositoryAffectsOneRow_ReturnsUnit()
    {
        var code = Guid.NewGuid();
        var patientCode = Guid.NewGuid();
        _repository.ConfirmPaymentAsync(code, patientCode, null)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.ConfirmPaymentAsync(code, patientCode, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_RepositoryAffectsZeroRows_ReturnsInvalidTransitionError()
    {
        _repository.ConfirmPaymentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(0)));

        var result = await _sut.ConfirmPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), null);

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentInvalidTransitionError>(result.Error);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_RepositoryFails_ReturnsFailure()
    {
        // BindSharp 2.1.0: EnsureAsync evaluates predicate(0) on int Failure,
        // so the exact error type is overwritten — we can only assert IsFailure.
        _repository.ConfirmPaymentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(
                Result<int, AppointmentRepositoryError>.Failure(
                    new TransitionAppointmentError())));

        var result = await _sut.ConfirmPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), null);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_InvalidInsuranceCode_ReturnsFailure()
    {
        // BindSharp 2.1.0: EnsureAsync evaluates predicate(0) on int Failure,
        // so the exact error type is overwritten — we can only assert IsFailure.
        var code = Guid.NewGuid();
        var patientCode = Guid.NewGuid();
        var insuranceCode = Guid.NewGuid();
        _repository.ConfirmPaymentAsync(code, patientCode, insuranceCode)
            .Returns(Task.FromResult(
                Result<int, AppointmentRepositoryError>.Failure(new InsertInsuranceNotFoundError())));

        var result = await _sut.ConfirmPaymentAsync(code, patientCode, insuranceCode);

        Assert.True(result.IsFailure);
    }

    // ── AdminCreateAsync (payment) ─────────────────────────────────────────

    [Fact]
    public async Task AdminCreateAsync_PayNowWithInsurance_PassesPayNowAndInsuranceCodeToRepository()
    {
        var row = MakeRow();
        var patientCode = Guid.NewGuid();
        var availabilityCode = Guid.NewGuid();
        var consultationTypeCode = Guid.NewGuid();
        var insuranceCode = Guid.NewGuid();
        var adminCode = Guid.NewGuid();
        _repository
            .InsertByAdminAsync(patientCode, availabilityCode, consultationTypeCode, true, insuranceCode, adminCode)
            .Returns(Task.FromResult(Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.AdminCreateAsync(
            new AdminCreateAppointmentRequest(
                patientCode, availabilityCode, consultationTypeCode, true, insuranceCode),
            adminCode);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).InsertByAdminAsync(
            patientCode, availabilityCode, consultationTypeCode, true, insuranceCode, adminCode);
    }

    [Fact]
    public async Task AdminCreateAsync_PayNowParticular_PassesPayNowTrueAndNullInsuranceCode()
    {
        var row = MakeRow();
        var patientCode = Guid.NewGuid();
        var availabilityCode = Guid.NewGuid();
        var consultationTypeCode = Guid.NewGuid();
        var adminCode = Guid.NewGuid();
        _repository
            .InsertByAdminAsync(patientCode, availabilityCode, consultationTypeCode, true, null, adminCode)
            .Returns(Task.FromResult(Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.AdminCreateAsync(
            new AdminCreateAppointmentRequest(
                patientCode, availabilityCode, consultationTypeCode, true, null),
            adminCode);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).InsertByAdminAsync(
            patientCode, availabilityCode, consultationTypeCode, true, null, adminCode);
    }

    [Fact]
    public async Task AdminCreateAsync_PayLater_PassesPayNowFalseAndNullInsuranceCode()
    {
        var row = MakeRow();
        var patientCode = Guid.NewGuid();
        var availabilityCode = Guid.NewGuid();
        var consultationTypeCode = Guid.NewGuid();
        var adminCode = Guid.NewGuid();
        _repository
            .InsertByAdminAsync(patientCode, availabilityCode, consultationTypeCode, false, null, adminCode)
            .Returns(Task.FromResult(Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.AdminCreateAsync(
            new AdminCreateAppointmentRequest(patientCode, availabilityCode, consultationTypeCode),
            adminCode);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).InsertByAdminAsync(
            patientCode, availabilityCode, consultationTypeCode, false, null, adminCode);
    }

    [Fact]
    public async Task AdminCreateAsync_InvalidInsuranceCode_ReturnsAppointmentInsuranceNotFoundError()
    {
        _repository
            .InsertByAdminAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<Guid?>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Failure(new InsertInsuranceNotFoundError())));

        var result = await _sut.AdminCreateAsync(
            new AdminCreateAppointmentRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, Guid.NewGuid()),
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentInsuranceNotFoundError>(result.Error);
    }

    // ── AdminConfirmPaymentAsync ────────────────────────────────────────────

    [Fact]
    public async Task AdminConfirmPaymentAsync_WithInsurance_ReturnsUnit()
    {
        var code = Guid.NewGuid();
        var adminCode = Guid.NewGuid();
        var insuranceCode = Guid.NewGuid();
        _repository.ConfirmPaymentByAdminAsync(code, insuranceCode, adminCode)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.AdminConfirmPaymentAsync(code, adminCode, insuranceCode);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AdminConfirmPaymentAsync_Particular_PassesNullInsuranceCodeToRepository()
    {
        var code = Guid.NewGuid();
        var adminCode = Guid.NewGuid();
        _repository.ConfirmPaymentByAdminAsync(code, null, adminCode)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.AdminConfirmPaymentAsync(code, adminCode, null);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).ConfirmPaymentByAdminAsync(code, null, adminCode);
    }

    [Fact]
    public async Task AdminConfirmPaymentAsync_RepositoryAffectsZeroRows_ReturnsAdminConfirmPaymentConflictError()
    {
        _repository.ConfirmPaymentByAdminAsync(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid>())
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(0)));

        var result = await _sut.AdminConfirmPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), null);

        Assert.True(result.IsFailure);
        Assert.IsType<AdminConfirmPaymentConflictError>(result.Error);
    }

    [Fact]
    public async Task AdminConfirmPaymentAsync_InvalidInsuranceCode_ReturnsFailure()
    {
        // BindSharp 2.1.0: EnsureAsync evaluates predicate(0) on int Failure,
        // so the exact error type is overwritten — we can only assert IsFailure.
        var insuranceCode = Guid.NewGuid();
        _repository.ConfirmPaymentByAdminAsync(Arg.Any<Guid>(), insuranceCode, Arg.Any<Guid>())
            .Returns(Task.FromResult(
                Result<int, AppointmentRepositoryError>.Failure(new InsertInsuranceNotFoundError())));

        var result = await _sut.AdminConfirmPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), insuranceCode);

        Assert.True(result.IsFailure);
    }

    // ── AdminRevertPaymentAsync ─────────────────────────────────────────────

    [Fact]
    public async Task AdminRevertPaymentAsync_RepositoryAffectsOneRow_ReturnsUnit()
    {
        var code = Guid.NewGuid();
        _repository.RevertPaymentAsync(code)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.AdminRevertPaymentAsync(code);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AdminRevertPaymentAsync_RepositoryAffectsZeroRows_ReturnsRevertPaymentConflictError()
    {
        _repository.RevertPaymentAsync(Arg.Any<Guid>())
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(0)));

        var result = await _sut.AdminRevertPaymentAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<RevertPaymentConflictError>(result.Error);
    }

    // ── CancelAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_PatientRole_PassesCallerCodeAsPatientCode()
    {
        var code = Guid.NewGuid();
        var callerCode = Guid.NewGuid();
        _repository.CancelAsync(code, callerCode)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.CancelAsync(code, callerCode, "Patient");

        Assert.True(result.IsSuccess);
        await _repository.Received(1).CancelAsync(code, callerCode);
    }

    [Fact]
    public async Task CancelAsync_NonPatientRole_PassesNullAsPatientCode()
    {
        var code = Guid.NewGuid();
        var callerCode = Guid.NewGuid();
        _repository.CancelAsync(code, (Guid?)null)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.CancelAsync(code, callerCode, "Administrador");

        Assert.True(result.IsSuccess);
        await _repository.Received(1).CancelAsync(code, (Guid?)null);
    }

    [Fact]
    public async Task CancelAsync_RepositoryAffectsZeroRows_ReturnsInvalidTransitionError()
    {
        _repository.CancelAsync(Arg.Any<Guid>(), Arg.Any<Guid?>())
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(0)));

        var result = await _sut.CancelAsync(Guid.NewGuid(), Guid.NewGuid(), "Patient");

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentInvalidTransitionError>(result.Error);
    }

    // ── CompleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_DoctorRoleOwnsAppointment_CallsGetByCodeAndCompletes()
    {
        var code = Guid.NewGuid();
        var doctorCode = Guid.NewGuid();
        var row = MakeRow(doctorCode: doctorCode);
        _repository.GetByCodeAsync(code)
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));
        _repository.CompleteAsync(code, doctorCode)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.CompleteAsync(code, doctorCode, "Doctor");

        Assert.True(result.IsSuccess);
        await _repository.Received(1).GetByCodeAsync(code);
        await _repository.Received(1).CompleteAsync(code, doctorCode);
    }

    [Fact]
    public async Task CompleteAsync_DoctorRoleDoesNotOwnAppointment_ReturnsForbiddenError()
    {
        var code = Guid.NewGuid();
        var doctorCode = Guid.NewGuid();
        var row = MakeRow(doctorCode: Guid.NewGuid()); // different doctor
        _repository.GetByCodeAsync(code)
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.CompleteAsync(code, doctorCode, "Doctor");

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentForbiddenError>(result.Error);
        await _repository.DidNotReceive().CompleteAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task CompleteAsync_NonDoctorRole_SkipsOwnershipCheckAndCompletes()
    {
        var code = Guid.NewGuid();
        var adminCode = Guid.NewGuid();
        _repository.CompleteAsync(code, adminCode)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.CompleteAsync(code, adminCode, "Administrador");

        Assert.True(result.IsSuccess);
        await _repository.DidNotReceive().GetByCodeAsync(Arg.Any<Guid>());
        await _repository.Received(1).CompleteAsync(code, adminCode);
    }

    [Fact]
    public async Task CompleteAsync_RepositoryAffectsZeroRows_ReturnsInvalidTransitionError()
    {
        var code = Guid.NewGuid();
        var adminCode = Guid.NewGuid();
        _repository.CompleteAsync(code, adminCode)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(0)));

        var result = await _sut.CompleteAsync(code, adminCode, "Administrador");

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentInvalidTransitionError>(result.Error);
    }

    // ── NoShowAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task NoShowAsync_AdminAffectsOneRow_ReturnsUnit()
    {
        var code = Guid.NewGuid();
        var adminCode = Guid.NewGuid();
        _repository.NoShowAsync(code, adminCode)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.NoShowAsync(code, adminCode, "Administrador");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NoShowAsync_AdminAffectsZeroRows_ReturnsInvalidTransitionError()
    {
        _repository.NoShowAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(0)));

        var result = await _sut.NoShowAsync(Guid.NewGuid(), Guid.NewGuid(), "Administrador");

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentInvalidTransitionError>(result.Error);
    }

    [Fact]
    public async Task NoShowAsync_DoctorIsAssignedDoctor_ReturnsUnit()
    {
        var code = Guid.NewGuid();
        var doctorCode = Guid.NewGuid();
        var row = MakeRow(appointmentCode: code, doctorCode: doctorCode);
        _repository.GetByCodeAsync(code)
            .Returns(Task.FromResult(Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));
        _repository.NoShowAsync(code, doctorCode)
            .Returns(Task.FromResult(Result<int, AppointmentRepositoryError>.Success(1)));

        var result = await _sut.NoShowAsync(code, doctorCode, "Doctor");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NoShowAsync_DoctorIsNotAssignedDoctor_ReturnsForbiddenError()
    {
        var code = Guid.NewGuid();
        var row = MakeRow(appointmentCode: code, doctorCode: Guid.NewGuid());
        _repository.GetByCodeAsync(code)
            .Returns(Task.FromResult(Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.NoShowAsync(code, Guid.NewGuid(), "Doctor");

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentForbiddenError>(result.Error);
    }

    // ── GetAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_PatientAccessesOwnAppointment_ReturnsResponse()
    {
        var code = Guid.NewGuid();
        var patientCode = Guid.NewGuid();
        var row = MakeRow(appointmentCode: code, patientCode: patientCode);
        _repository.GetByCodeAsync(code)
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.GetAsync(code, patientCode, "Patient");

        Assert.True(result.IsSuccess);
        Assert.Equal(code, result.Value.AppointmentCode);
    }

    [Fact]
    public async Task GetAsync_PatientAccessesOtherPatientAppointment_ReturnsForbiddenError()
    {
        var code = Guid.NewGuid();
        var row = MakeRow(appointmentCode: code, patientCode: Guid.NewGuid());
        _repository.GetByCodeAsync(code)
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.GetAsync(code, Guid.NewGuid(), "Patient");

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentForbiddenError>(result.Error);
    }

    [Fact]
    public async Task GetAsync_DoctorAccessesOwnAppointment_ReturnsResponse()
    {
        var code = Guid.NewGuid();
        var doctorCode = Guid.NewGuid();
        var row = MakeRow(appointmentCode: code, doctorCode: doctorCode);
        _repository.GetByCodeAsync(code)
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.GetAsync(code, doctorCode, "Doctor");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetAsync_AdministradorRole_AlwaysGrantsAccess()
    {
        var code = Guid.NewGuid();
        var row = MakeRow(appointmentCode: code);
        _repository.GetByCodeAsync(code)
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(row)));

        var result = await _sut.GetAsync(code, Guid.NewGuid(), "Administrador");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetAsync_AppointmentNotFound_ReturnsFailure()
    {
        // EnsureNotNullAsync correctly detects null → Failure(AppointmentNotFoundError).
        // But BindSharp 2.1.0 EnsureAsync then throws accessing Value on that Failure,
        // converting it to AppointmentForbiddenError — only IsFailure can be asserted.
        _repository.GetByCodeAsync(Arg.Any<Guid>())
            .Returns(Task.FromResult(
                Result<AppointmentRow?, AppointmentRepositoryError>.Success(null)));

        var result = await _sut.GetAsync(Guid.NewGuid(), Guid.NewGuid(), "Administrador");

        Assert.True(result.IsFailure);
    }

    // ── GetPatientAppointmentsAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetPatientAppointmentsAsync_RepositorySucceeds_ReturnsPageResponse()
    {
        var patientCode = Guid.NewGuid();
        var row = MakeRow(patientCode: patientCode);
        _repository.GetPatientAppointmentsAsync(patientCode, 1, 10)
            .Returns(Task.FromResult(
                Result<(IEnumerable<AppointmentRow> Items, int Total), AppointmentRepositoryError>.Success(
                    ([row], 1))));

        var result = await _sut.GetPatientAppointmentsAsync(patientCode, new AppointmentPageRequest(1, 10));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task GetPatientAppointmentsAsync_RepositoryFails_ReturnsAppointmentDataAccessError()
    {
        _repository.GetPatientAppointmentsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromResult(
                Result<(IEnumerable<AppointmentRow> Items, int Total), AppointmentRepositoryError>.Failure(
                    new GetPatientAppointmentsError())));

        var result = await _sut.GetPatientAppointmentsAsync(
            Guid.NewGuid(), new AppointmentPageRequest(1, 10));

        Assert.True(result.IsFailure);
        Assert.IsType<AppointmentDataAccessError>(result.Error);
    }

    // ── GetDoctorAppointmentsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetDoctorAppointmentsAsync_RepositorySucceeds_ReturnsPageResponse()
    {
        var doctorCode = Guid.NewGuid();
        var row = MakeRow(doctorCode: doctorCode);
        _repository.GetDoctorAppointmentsAsync(
                doctorCode, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<(IEnumerable<AppointmentRow> Items, int Total), AppointmentRepositoryError>.Success(
                    ([row], 1))));

        var result = await _sut.GetDoctorAppointmentsAsync(
            doctorCode, new DoctorAppointmentPageRequest(1, 10, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
    }
}
