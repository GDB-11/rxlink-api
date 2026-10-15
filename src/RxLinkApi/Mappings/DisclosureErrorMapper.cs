using Application.Core.DTOs.Disclosure.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class DisclosureErrorMapper : IErrorHttpMapper<DisclosureError>
{
    public IActionResult MapToHttp(DisclosureError error) =>
        error switch
        {
            DisclosurePatientNotFoundError => new NotFoundObjectResult(new { message = error.Message }),

            DisclosureAccessDeniedError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            DisclosureInvalidError => new BadRequestObjectResult(new { message = error.Message }),

            DisclosureDataAccessError dataError => new ObjectResult(new { message = dataError.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la divulgación."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
