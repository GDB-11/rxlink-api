using Application.Core.DTOs.Availability.Errors;
using Application.Core.DTOs.Availability.Request;
using Application.Core.DTOs.Availability.Response;
using Application.Core.Interfaces.Availability;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Availability;
using Infrastructure.Core.Models.Availability;

namespace Application.Core.Services.Availability;

public sealed class AvailabilityService : IAvailability
{
    private readonly IAvailabilityRepository _repository;

    public AvailabilityService(IAvailabilityRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AvailabilityResponse>, AvailabilityError>> CreateAsync(
        Guid doctorCode, CreateAvailabilityRequest request, Guid createdByUserCode)
    {
        var parsedSlots = new List<(DateOnly Date, TimeOnly StartTime)>(request.Slots.Count);
        foreach (var slot in request.Slots)
        {
            if (!TimeOnly.TryParseExact(slot.StartTime, "HH:mm", out TimeOnly time))
                return new AvailabilityInvalidTimeFormatError(slot.StartTime);
            parsedSlots.Add((slot.Date, time));
        }

        var doctorResult = await _repository.GetDoctorUserIdAsync(doctorCode)
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception));

        if (doctorResult.IsFailure)
            return doctorResult.Error;
        if (!doctorResult.Value.HasValue)
            return new AvailabilityDoctorNotFoundError();

        int doctorUserId = doctorResult.Value.Value;

        var created = new List<AvailabilityRow>();
        foreach (var (date, startTime) in parsedSlots)
        {
            var insertResult = await _repository.InsertOneAsync(doctorUserId, date, startTime, createdByUserCode)
                .MapErrorAsync(AvailabilityError (e) =>
                    new AvailabilityDataAccessError(e.Message, e.Details, e.Exception));

            if (insertResult.IsFailure)
                return insertResult.Error;
            if (insertResult.Value is not null)
                created.Add(insertResult.Value);
        }

        return created.Select(MapToResponse).ToList();
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AvailabilityResponse>, AvailabilityError>> GetByDoctorAndMonthAsync(
        Guid doctorCode, GetAvailabilityRequest request)
    {
        var monthDate = DateOnly.Parse($"{request.Month}-01");
        var startDate = new DateOnly(monthDate.Year, monthDate.Month, 1);
        var endDate = startDate.AddMonths(1);

        return _repository.GetByDoctorAndMonthAsync(doctorCode, startDate, endDate)
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(rows => rows.Select(MapToResponse));
    }

    /// <inheritdoc/>
    public async Task<Result<Unit, AvailabilityError>> DeleteAsync(Guid availabilityCode, Guid deletedByUserCode)
    {
        var isBookedResult = await _repository.GetIsBookedAsync(availabilityCode)
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception));

        if (isBookedResult.IsFailure)
            return isBookedResult.Error;
        if (isBookedResult.Value is null)
            return new AvailabilityNotFoundError();
        if (isBookedResult.Value is true)
            return new AvailabilityAlreadyBookedError();

        return await _repository.SoftDeleteAsync(availabilityCode, deletedByUserCode)
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
            .EnsureAsync(rows => rows > 0, new AvailabilityNotFoundError())
            .MapAsync(_ => Unit.Value);
    }

    /// <inheritdoc/>
    public Task<Result<AvailableDatesResponse, AvailabilityError>> GetAvailableDatesAsync(Guid doctorCode) =>
        _repository.GetAvailableDatesAsync(doctorCode)
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(rows => new AvailableDatesResponse
            {
                DoctorCode = doctorCode,
                AvailableDates = rows.Select(r => r.Date).ToList()
            });

    /// <inheritdoc/>
    public Task<Result<AvailableSlotsResponse, AvailabilityError>> GetAvailableSlotsAsync(
        Guid doctorCode, AvailableSlotsRequest request) =>
        _repository.GetAvailableSlotsAsync(doctorCode, request.Date)
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(rows => new AvailableSlotsResponse
            {
                DoctorCode = doctorCode,
                Date = request.Date,
                Slots = rows.Select(MapToSlotItem).ToList()
            });

    private static AvailabilityResponse MapToResponse(AvailabilityRow row) =>
        new()
        {
            AvailabilityCode = row.DoctorAvailabilityCode,
            Date = row.Date,
            StartTime = row.StartTime.ToString("HH:mm"),
            IsBooked = row.IsBooked
        };

    private static AvailableSlotItem MapToSlotItem(AvailableSlotRow row) =>
        new()
        {
            AvailabilityCode = row.DoctorAvailabilityCode,
            Time = row.StartTime.ToString("h:mm tt").ToLower()
        };
}