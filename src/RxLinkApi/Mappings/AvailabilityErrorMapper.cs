using Application.Core.DTOs.Availability.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class AvailabilityErrorMapper : IErrorHttpMapper<AvailabilityError>
{
    public IActionResult MapToHttp(AvailabilityError error) =>
        error switch
        {
            AvailabilityDoctorNotFoundError => new NotFoundObjectResult(new { message = error.Message }),
            AvailabilityNotFoundError => new NotFoundObjectResult(new { message = error.Message }),
            AvailabilityAlreadyBookedError => new ConflictObjectResult(new { message = error.Message }),
            AvailabilityInvalidTimeFormatError => new BadRequestObjectResult(new { message = error.Message }),

            AvailabilityDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de disponibilidad."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}