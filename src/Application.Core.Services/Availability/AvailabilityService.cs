using Application.Core.DTOs.Availability.Errors;
using Application.Core.DTOs.Availability.Request;
using Application.Core.DTOs.Availability.Response;
using Application.Core.Interfaces.Availability;
using Application.Core.Interfaces.Shared;
using BindSharp;
using BindSharp.Extensions;
using Common.Helpers;
using Infrastructure.Core.Interfaces.Availability;
using Infrastructure.Core.Models.Availability;

namespace Application.Core.Services.Availability;

public sealed class AvailabilityService : IAvailability
{
    private readonly IAvailabilityRepository _repository;
    private readonly ITimeProvider _timeProvider;

    public AvailabilityService(IAvailabilityRepository repository, ITimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
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

        return await _repository.GetDoctorUserIdAsync(doctorCode)
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
            .EnsureAsync(id => id.HasValue, new AvailabilityDoctorNotFoundError())
            .MapAsync(id => id!.Value)
            .BindAsync(async doctorUserId =>
            {
                Task<Result<List<AvailabilityRow>, AvailabilityError>> accumulated =
                    Task.FromResult(
                        Result<List<AvailabilityRow>, AvailabilityError>.Success(new List<AvailabilityRow>()));

                foreach (var (date, startTime) in parsedSlots)
                    accumulated = accumulated.BindAsync(list =>
                        _repository.InsertOneAsync(doctorUserId, date.ToDateTime(), startTime.ToTimeSpan(),
                                createdByUserCode)
                            .MapErrorAsync(AvailabilityError (e) =>
                                new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
                            .MapAsync(row =>
                            {
                                if (row is not null) list.Add(row);
                                return list;
                            }));

                var now = _timeProvider.UtcNow;
                return (await accumulated).Map(list => list.Select(row => MapToResponse(row, now)));
            });
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<AvailabilityResponse>, AvailabilityError>> GetByDoctorAndMonthAsync(
        Guid doctorCode, GetAvailabilityRequest request)
    {
        var monthDate = DateOnly.Parse($"{request.Month}-01");
        var startDate = new DateOnly(monthDate.Year, monthDate.Month, 1);
        var endDate = startDate.AddMonths(1);

        var now = _timeProvider.UtcNow;
        return _repository.GetByDoctorAndMonthAsync(doctorCode, startDate.ToDateTime(), endDate.ToDateTime())
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(rows => rows.Select(row => MapToResponse(row, now)));
    }

    /// <inheritdoc/>
    public Task<Result<Unit, AvailabilityError>> DeleteAsync(Guid availabilityCode, Guid deletedByUserCode)
    {
        var now = _timeProvider.UtcNow;
        return _repository.GetSlotForDeletionAsync(availabilityCode)
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
            .EnsureAsync(row => row is not null, new AvailabilityNotFoundError())
            .EnsureAsync(row => !row!.IsBooked, new AvailabilityAlreadyBookedError())
            .EnsureAsync(row => row!.Date.ToDateTime(row.StartTime) > now, new AvailabilitySlotInPastError())
            .EnsureAsync(row => (row!.Date.ToDateTime(row.StartTime) - now).TotalHours > 5,
                new AvailabilitySlotTooCloseError())
            .BindAsync(_ => _repository.SoftDeleteAsync(availabilityCode, deletedByUserCode)
                .MapErrorAsync(AvailabilityError (e) =>
                    new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
                .EnsureAsync(rows => rows > 0, new AvailabilityNotFoundError())
                .MapAsync(_ => Unit.Value));
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
        _repository.GetAvailableSlotsAsync(doctorCode, request.Date.ToDateTime())
            .MapErrorAsync(AvailabilityError (e) => new AvailabilityDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(rows => new AvailableSlotsResponse
            {
                DoctorCode = doctorCode,
                Date = request.Date,
                Slots = rows.Select(MapToSlotItem).ToList()
            });

    private static AvailabilityResponse MapToResponse(AvailabilityRow row, DateTime now)
    {
        var slotDateTime = row.Date.ToDateTime(row.StartTime);
        return new()
        {
            AvailabilityCode = row.DoctorAvailabilityCode,
            Date             = row.Date,
            StartTime        = row.StartTime.ToString("HH:mm"),
            IsBooked         = row.IsBooked,
            CanDelete        = !row.IsBooked
                               && slotDateTime > now
                               && (slotDateTime - now).TotalHours > 5
        };
    }

    private static AvailableSlotItem MapToSlotItem(AvailableSlotRow row) =>
        new()
        {
            AvailabilityCode = row.DoctorAvailabilityCode,
            Time = row.StartTime.ToString("h:mm tt").ToLower()
        };
}