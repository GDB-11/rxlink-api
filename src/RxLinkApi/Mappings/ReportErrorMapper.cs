using Application.Core.DTOs.Report.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class ReportErrorMapper : IErrorHttpMapper<ReportError>
{
    public IActionResult MapToHttp(ReportError error) =>
        error switch
        {
            ReportInvalidRangeError => new BadRequestObjectResult(new
            {
                message = error.Message
            }),

            ReportDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de reportes."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
