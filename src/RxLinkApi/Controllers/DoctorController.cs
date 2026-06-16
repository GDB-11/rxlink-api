using Application.Core.DTOs.Availability.Errors;
using Application.Core.DTOs.Availability.Request;
using Application.Core.DTOs.Availability.Response;
using Application.Core.Interfaces.Availability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/doctor")]
public sealed class DoctorController : FunctionalController
{
    private readonly IAvailability _availabilityService;
    private readonly IErrorHttpMapper<AvailabilityError> _errorMapper;

    public DoctorController(
        IAvailability availabilityService,
        IErrorHttpMapper<AvailabilityError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _availabilityService = availabilityService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Creates one or more availability slots for a doctor. Duplicate slots are silently ignored.
    /// </summary>
    [HttpPost("{code:guid}/availability")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> CreateAvailability(Guid code, [FromBody] CreateAvailabilityRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _availabilityService.CreateAsync(code, request, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(CreateAvailability),
            successMapper: slots => Created($"api/doctor/{code}/availability", slots)
        );

    /// <summary>
    /// Returns all slots (free and booked) for a doctor in the specified month.
    /// </summary>
    [HttpGet("{code:guid}/availability")]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAvailability(Guid code, [FromQuery] GetAvailabilityRequest request) =>
        ExecuteAsync(
            operation: () => _availabilityService.GetByDoctorAndMonthAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(GetAvailability)
        );

    /// <summary>
    /// Soft-deletes a non-booked availability slot.
    /// Returns 404 when the slot does not exist or was already deleted.
    /// Returns 409 when the slot is already booked.
    /// </summary>
    [HttpDelete("/api/availability/{code:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> DeleteAvailability(Guid code) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _availabilityService.DeleteAsync(code, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(DeleteAvailability),
            successMapper: _ => NoContent()
        );

    /// <summary>
    /// Returns dates with at least one free slot for the doctor, from today through today + 30 days.
    /// Public endpoint — no authentication required.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{code:guid}/available-dates")]
    [ProducesResponseType(typeof(AvailableDatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAvailableDates(Guid code) =>
        ExecuteAsync(
            operation: () => _availabilityService.GetAvailableDatesAsync(code),
            errorMapper: _errorMapper,
            operationName: nameof(GetAvailableDates)
        );

    /// <summary>
    /// Returns free time slots for a doctor on the specified date.
    /// Returns an empty list (not 404) when no slots are available.
    /// Public endpoint — no authentication required.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{code:guid}/available-slots")]
    [ProducesResponseType(typeof(AvailableSlotsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAvailableSlots(Guid code, [FromQuery] AvailableSlotsRequest request) =>
        ExecuteAsync(
            operation: () => _availabilityService.GetAvailableSlotsAsync(code, request),
            errorMapper: _errorMapper,
            operationName: nameof(GetAvailableSlots)
        );
}