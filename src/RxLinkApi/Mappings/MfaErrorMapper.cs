using Application.Core.DTOs.Mfa.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class MfaErrorMapper : IErrorHttpMapper<MfaError>
{
    public IActionResult MapToHttp(MfaError error) =>
        error switch
        {
            MfaAlreadyEnabledError => new ConflictObjectResult(new { message = error.Message }),

            MfaNotPendingError or MfaInvalidCodeError => new BadRequestObjectResult(new { message = error.Message }),

            MfaNotEnabledError => new NotFoundObjectResult(new { message = error.Message }),

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de MFA."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
