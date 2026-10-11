using Application.Core.DTOs.Person.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class PersonErrorMapper : IErrorHttpMapper<PersonError>
{
    public IActionResult MapToHttp(PersonError error) =>
        error switch
        {
            PersonNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            PersonDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            PersonCatalogValidationError catalogError => CatalogValidationHttp.Map(catalogError.Inner),

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de personas."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}