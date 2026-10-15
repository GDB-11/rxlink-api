using Application.Core.DTOs.Specialty.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class SpecialtyErrorMapper : IErrorHttpMapper<SpecialtyError>
{
    public IActionResult MapToHttp(SpecialtyError error) =>
        error switch
        {
            SpecialtyNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            SpecialtyCatalogUnavailableError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            },

            SpecialtyDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de especialidades."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}