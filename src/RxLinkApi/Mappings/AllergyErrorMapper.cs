using Application.Core.DTOs.Allergy.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class AllergyErrorMapper : IErrorHttpMapper<AllergyError>
{
    public IActionResult MapToHttp(AllergyError error) =>
        error switch
        {
            AllergyNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            AllergyDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de alergias."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}