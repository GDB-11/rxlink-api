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

            // The code lets the client offer the stock override instead of a plain error.
            PrescriptionInsufficientStockError => new ConflictObjectResult(new
            {
                message = error.Message,
                code = "insufficient_stock"
            }),

            PrescriptionDuplicateError or PrescriptionInvalidStatusError or PrescriptionInvalidTransitionError =>
                new ConflictObjectResult(new
                {
                    message = error.Message
                }),

            PrescriptionDispenseLinesInvalidError => new BadRequestObjectResult(new { message = error.Message }),

            PrescriptionPatientForbiddenError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status403Forbidden
            },

            PrescriptionDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            PrescriptionCatalogValidationError catalogError => CatalogValidationHttp.Map(catalogError.Inner),

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de la receta."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}