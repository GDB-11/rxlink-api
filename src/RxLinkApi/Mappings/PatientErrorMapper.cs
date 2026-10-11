using Application.Core.DTOs.Patient.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class PatientErrorMapper : IErrorHttpMapper<PatientError>
{
    public IActionResult MapToHttp(PatientError error) =>
        error switch
        {
            PatientNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            PatientAccessDeniedError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            PatientPersonNotFoundError => new BadRequestObjectResult(new
            {
                message = error.Message
            }),

            PatientAllergyNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            PatientDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            PatientCatalogValidationError catalogError => CatalogValidationHttp.Map(catalogError.Inner),

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud del paciente."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}