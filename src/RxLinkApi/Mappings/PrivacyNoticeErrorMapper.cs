using Application.Core.DTOs.PrivacyNotice.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class PrivacyNoticeErrorMapper : IErrorHttpMapper<PrivacyNoticeError>
{
    public IActionResult MapToHttp(PrivacyNoticeError error) =>
        error switch
        {
            PrivacyNoticePatientNotFoundError or PrivacyNoticeNotPublishedError =>
                new NotFoundObjectResult(new { message = error.Message }),

            PrivacyNoticeAccessDeniedError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            PrivacyNoticeInvalidError => new BadRequestObjectResult(new { message = error.Message }),

            PrivacyNoticeDataAccessError dataError => new ObjectResult(new { message = dataError.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar el aviso de privacidad."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
