using Application.Core.DTOs.Diagnostic.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class DiagnosticErrorMapper : IErrorHttpMapper<DiagnosticError>
{
    public IActionResult MapToHttp(DiagnosticError error) =>
        error switch
        {
            DiagnosticNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            DiagnosticInvalidAppointmentError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            },

            DiagnosticDuplicateError
                or DiagnosticInvalidTransitionError => new ConflictObjectResult(new
                {
                    message = error.Message
                }),

            DiagnosticDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud del diagnóstico."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}