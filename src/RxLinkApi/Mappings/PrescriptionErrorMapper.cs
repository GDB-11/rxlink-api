using Application.Core.DTOs.Prescription.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class PrescriptionErrorMapper : IErrorHttpMapper<PrescriptionError>
{
    public IActionResult MapToHttp(PrescriptionError error) =>
        error switch
        {
            PrescriptionNotFoundError or PrescriptionDiagnosticNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            PrescriptionDuplicateError or PrescriptionInvalidStatusError or PrescriptionInvalidTransitionError =>
                new ConflictObjectResult(new
                {
                    message = error.Message
                }),

            PrescriptionDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de la receta."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
