using Application.Core.DTOs.CommunicationPreference.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class CommunicationPreferenceErrorMapper : IErrorHttpMapper<CommunicationPreferenceError>
{
    public IActionResult MapToHttp(CommunicationPreferenceError error) =>
        error switch
        {
            CommunicationPreferencePatientNotFoundError => new NotFoundObjectResult(new { message = error.Message }),

            CommunicationPreferenceAccessDeniedError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            CommunicationPreferenceInvalidError => new BadRequestObjectResult(new { message = error.Message }),

            CommunicationPreferenceDataAccessError dataError => new ObjectResult(new { message = dataError.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar las preferencias de comunicación."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
