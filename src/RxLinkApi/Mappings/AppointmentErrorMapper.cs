using Application.Core.DTOs.Appointment.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class AppointmentErrorMapper : IErrorHttpMapper<AppointmentError>
{
    public IActionResult MapToHttp(AppointmentError error) =>
        error switch
        {
            AppointmentNotFoundError
                or AppointmentPatientNotFoundError
                or AppointmentSlotNotFoundError
                or AppointmentConsultationTypeNotFoundError => new NotFoundObjectResult(new
                {
                    message = error.Message
                }),

            AppointmentForbiddenError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            AppointmentSlotExpiredError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            },

            AppointmentSlotAlreadyBookedError
                or AppointmentInvalidTransitionError
                or AdminConfirmPaymentConflictError
                or RevertPaymentConflictError => new ConflictObjectResult(new
                {
                    message = error.Message
                }),

            AppointmentDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de la cita."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}