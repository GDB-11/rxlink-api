using Application.Core.DTOs.Amendment.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class AmendmentErrorMapper : IErrorHttpMapper<AmendmentError>
{
    public IActionResult MapToHttp(AmendmentError error) =>
        error switch
        {
            AmendmentNotFoundError or AmendmentPatientNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            AmendmentAccessDeniedError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            AmendmentInvalidError => new BadRequestObjectResult(new { message = error.Message }),

            AmendmentAlreadyDecidedError => new ConflictObjectResult(new { message = error.Message }),

            AmendmentDataAccessError dataError => new ObjectResult(new { message = dataError.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de enmienda."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
