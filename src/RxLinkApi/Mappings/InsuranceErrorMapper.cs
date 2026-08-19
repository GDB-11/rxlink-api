using Application.Core.DTOs.Insurance.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class InsuranceErrorMapper : IErrorHttpMapper<InsuranceError>
{
    public IActionResult MapToHttp(InsuranceError error) =>
        error switch
        {
            InsuranceNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            InsuranceDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de seguros."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
