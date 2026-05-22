using Application.Core.DTOs.Medication.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class MedicationErrorMapper : IErrorHttpMapper<MedicationError>
{
    public IActionResult MapToHttp(MedicationError error) =>
        error switch
        {
            MedicationNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            MedicationDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de medicamentos."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
