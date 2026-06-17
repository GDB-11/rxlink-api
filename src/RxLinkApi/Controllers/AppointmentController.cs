using System.Security.Claims;
using Application.Core.DTOs.Appointment.Errors;
using Application.Core.DTOs.Appointment.Request;
using Application.Core.DTOs.Appointment.Response;
using Application.Core.Interfaces.Appointment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[ApiController]
[Route("api")]
public sealed class AppointmentController : FunctionalController
{
    private readonly IAppointment _appointmentService;
    private readonly IErrorHttpMapper<AppointmentError> _errorMapper;

    public AppointmentController(
        IAppointment appointmentService,
        IErrorHttpMapper<AppointmentError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _appointmentService = appointmentService;
        _errorMapper = errorMapper;
    }

    /// <summary>Creates a new appointment. Locks the availability slot atomically.</summary>
    [HttpPost("appointment")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromBody] CreateAppointmentRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _appointmentService.CreateAsync(request, patientCode),
            errorMapper: _errorMapper,
            operationName: nameof(Create),
            successMapper: appointment => Created($"api/appointment/{appointment.AppointmentCode}", appointment)
        );

    /// <summary>Transitions PendientePago → Confirmado.</summary>
    [HttpPatch("appointment/{code:guid}/confirm-payment")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> ConfirmPayment(Guid code) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _appointmentService.ConfirmPaymentAsync(code, patientCode),
            errorMapper: _errorMapper,
            operationName: nameof(ConfirmPayment),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Transitions PendientePago/Confirmado → Cancelado and releases the slot.
    /// Patient can only cancel their own appointments; Admin can cancel any.
    /// </summary>
    [HttpPatch("appointment/{code:guid}/cancel")]
    [Authorize(Roles = "Patient,Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Cancel(Guid code)
    {
        string? role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role))
            return Task.FromResult<IActionResult>(Unauthorized());

        Guid callerCode;
        if (role == "Patient")
        {
            if (!Guid.TryParse(User.FindFirst("patient_code")?.Value, out callerCode))
                return Task.FromResult<IActionResult>(Unauthorized());
        }
        else
        {
            if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out callerCode))
                return Task.FromResult<IActionResult>(Unauthorized());
        }

        return ExecuteAsync(
            operation: () => _appointmentService.CancelAsync(code, callerCode, role),
            errorMapper: _errorMapper,
            operationName: nameof(Cancel),
            successMapper: _ => NoContent()
        );
    }

    /// <summary>
    /// Transitions Confirmado → Completado.
    /// Doctor must be the assigned doctor; Admin can complete any.
    /// </summary>
    [HttpPatch("appointment/{code:guid}/complete")]
    [Authorize(Roles = "Doctor,Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Complete(Guid code)
    {
        string? role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role))
            return Task.FromResult<IActionResult>(Unauthorized());

        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid userCode))
            return Task.FromResult<IActionResult>(Unauthorized());

        return ExecuteAsync(
            operation: () => _appointmentService.CompleteAsync(code, userCode, role),
            errorMapper: _errorMapper,
            operationName: nameof(Complete),
            successMapper: _ => NoContent()
        );
    }

    /// <summary>Transitions Confirmado → NoAsistio. Admin only.</summary>
    [HttpPatch("appointment/{code:guid}/no-show")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> NoShow(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _appointmentService.NoShowAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(NoShow),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Returns appointment details.
    /// Patient must own it; Doctor must be the assigned doctor; Admin can see any.
    /// </summary>
    [HttpGet("appointment/{code:guid}")]
    [Authorize(Roles = "Patient,Doctor,Administrador")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetByCode(Guid code)
    {
        string? role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(role))
            return Task.FromResult<IActionResult>(Unauthorized());

        Guid callerCode;
        if (role == "Patient")
        {
            if (!Guid.TryParse(User.FindFirst("patient_code")?.Value, out callerCode))
                return Task.FromResult<IActionResult>(Unauthorized());
        }
        else
        {
            if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out callerCode))
                return Task.FromResult<IActionResult>(Unauthorized());
        }

        return ExecuteAsync(
            operation: () => _appointmentService.GetAsync(code, callerCode, role),
            errorMapper: _errorMapper,
            operationName: nameof(GetByCode)
        );
    }

    /// <summary>Returns the authenticated patient's appointments ordered by scheduledAt DESC.</summary>
    [HttpGet("patient/appointments")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(AppointmentPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPatientAppointments([FromQuery] AppointmentPageRequest request) =>
        ExecuteAuthenticatedPatientAsync(
            operation: patientCode => _appointmentService.GetPatientAppointmentsAsync(patientCode, request),
            errorMapper: _errorMapper,
            operationName: nameof(GetPatientAppointments)
        );

    /// <summary>Returns the authenticated doctor's appointments, ordered by scheduledAt ASC, with optional filters.</summary>
    [HttpGet("doctor/appointments")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(AppointmentPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetDoctorAppointments([FromQuery] DoctorAppointmentPageRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: doctorUserCode => _appointmentService.GetDoctorAppointmentsAsync(doctorUserCode, request),
            errorMapper: _errorMapper,
            operationName: nameof(GetDoctorAppointments)
        );

    /// <summary>Creates an appointment on behalf of a patient. Admin only.</summary>
    [HttpPost("admin/appointment")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> AdminCreate([FromBody] AdminCreateAppointmentRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: adminCode => _appointmentService.AdminCreateAsync(request, adminCode),
            errorMapper: _errorMapper,
            operationName: nameof(AdminCreate),
            successMapper: appt => Created($"api/appointment/{appt.AppointmentCode}", appt)
        );

    /// <summary>Transitions PendientePago → Confirmado. Admin only.</summary>
    [HttpPatch("appointment/{code:guid}/admin-confirm-payment")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> AdminConfirmPayment(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: _ => _appointmentService.AdminConfirmPaymentAsync(code),
            errorMapper: _errorMapper,
            operationName: nameof(AdminConfirmPayment),
            successMapper: _ => NoContent()
        );

    /// <summary>Transitions Confirmado → PendientePago. Admin only.</summary>
    [HttpPatch("appointment/{code:guid}/admin-revert-payment")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> AdminRevertPayment(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: _ => _appointmentService.AdminRevertPaymentAsync(code),
            errorMapper: _errorMapper,
            operationName: nameof(AdminRevertPayment),
            successMapper: _ => NoContent()
        );

    /// <summary>Returns a filtered, paginated list of all appointments. Admin only.</summary>
    [HttpGet("admin/appointments")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(AppointmentPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAdminAppointments([FromQuery] AdminAppointmentPageRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: _ => _appointmentService.GetAdminAppointmentsAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetAdminAppointments)
        );
}