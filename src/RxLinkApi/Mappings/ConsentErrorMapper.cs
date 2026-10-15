using Application.Core.DTOs.Consent.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class ConsentErrorMapper : IErrorHttpMapper<ConsentError>
{
    public IActionResult MapToHttp(ConsentError error) =>
        error switch
        {
            ConsentPatientNotFoundError or ConsentNotFoundError =>
                new NotFoundObjectResult(new { message = error.Message }),

            ConsentAccessDeniedError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            ConsentInvalidError => new BadRequestObjectResult(new { message = error.Message }),

            ConsentDataAccessError dataError => new ObjectResult(new { message = dataError.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar el consentimiento."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
