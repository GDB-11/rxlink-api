using Application.Core.DTOs.PatientAccess.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class PatientAccessErrorMapper : IErrorHttpMapper<PatientAccessError>
{
    public IActionResult MapToHttp(PatientAccessError error) =>
        error switch
        {
            PatientAccessNotPermittedError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            BreakGlassPatientNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            BreakGlassReportInvalidRangeError => new BadRequestObjectResult(new
            {
                message = error.Message
            }),

            PatientAccessDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de acceso a pacientes."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
