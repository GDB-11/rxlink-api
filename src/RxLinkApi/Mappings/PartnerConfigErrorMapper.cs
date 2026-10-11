using Application.Core.DTOs.PartnerConfig.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class PartnerConfigErrorMapper : IErrorHttpMapper<PartnerConfigError>
{
    public IActionResult MapToHttp(PartnerConfigError error) =>
        error switch
        {
            PartnerConfigUnavailableError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            },

            PartnerLogoNotFoundError => new NotFoundObjectResult(new { message = error.Message }),

            _ => new ObjectResult(new { message = "Error inesperado al recuperar la configuración del centro." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
