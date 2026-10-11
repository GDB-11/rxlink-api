using Application.Core.DTOs.Clinic.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class ClinicErrorMapper : IErrorHttpMapper<ClinicError>
{
    public IActionResult MapToHttp(ClinicError error) =>
        error switch
        {
            ClinicNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            ClinicDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud del centro de atención."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
