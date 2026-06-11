using Application.Core.DTOs.Availability.Errors;
using Application.Core.DTOs.Availability.Request;
using Application.Core.DTOs.Availability.Response;
using BindSharp;

namespace Application.Core.Interfaces.Availability;

public interface IAvailability
{
    Task<Result<IEnumerable<AvailabilityResponse>, AvailabilityError>> CreateAsync(
        Guid doctorCode, CreateAvailabilityRequest request, Guid createdByUserCode);

    Task<Result<IEnumerable<AvailabilityResponse>, AvailabilityError>> GetByDoctorAndMonthAsync(
        Guid doctorCode, GetAvailabilityRequest request);

    Task<Result<Unit, AvailabilityError>> DeleteAsync(Guid availabilityCode, Guid deletedByUserCode);

    Task<Result<AvailableDatesResponse, AvailabilityError>> GetAvailableDatesAsync(Guid doctorCode);

    Task<Result<AvailableSlotsResponse, AvailabilityError>> GetAvailableSlotsAsync(
        Guid doctorCode, AvailableSlotsRequest request);
}