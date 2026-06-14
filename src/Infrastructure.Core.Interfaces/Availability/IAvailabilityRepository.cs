using BindSharp;
using Infrastructure.Core.DTOs.Availability;
using Infrastructure.Core.Models.Availability;

namespace Infrastructure.Core.Interfaces.Availability;

public interface IAvailabilityRepository
{
    /// <summary>Returns the UserId of an active Doctor by UserCode, or null if not found.</summary>
    Task<Result<int?, AvailabilityRepositoryError>> GetDoctorUserIdAsync(Guid doctorCode);

    /// <summary>Inserts one slot. Returns null when ignored due to a duplicate conflict.</summary>
    Task<Result<AvailabilityRow?, AvailabilityRepositoryError>> InsertOneAsync(
        int doctorUserId, DateTime date, TimeOnly startTime, Guid createdByUserCode);

    /// <summary>Returns all non-deleted slots for a doctor in the given date range [startDate, endDate).</summary>
    Task<Result<IEnumerable<AvailabilityRow>, AvailabilityRepositoryError>> GetByDoctorAndMonthAsync(
        Guid doctorCode, DateTime startDate, DateTime endDate);

    /// <summary>Returns IsBooked for the slot, or null if not found or already soft-deleted.</summary>
    Task<Result<bool?, AvailabilityRepositoryError>> GetIsBookedAsync(Guid availabilityCode);

    /// <summary>Soft-deletes a non-booked slot. Returns the number of rows affected (0 = not found or already deleted/booked).</summary>
    Task<Result<int, AvailabilityRepositoryError>> SoftDeleteAsync(Guid availabilityCode, Guid deletedByUserCode);

    /// <summary>Returns distinct dates with at least one free slot, from today through today + 30 days.</summary>
    Task<Result<IEnumerable<AvailableDateRow>, AvailabilityRepositoryError>> GetAvailableDatesAsync(Guid doctorCode);

    /// <summary>Returns all free slots for a doctor on the specified date.</summary>
    Task<Result<IEnumerable<AvailableSlotRow>, AvailabilityRepositoryError>> GetAvailableSlotsAsync(
        Guid doctorCode, DateTime date);
}