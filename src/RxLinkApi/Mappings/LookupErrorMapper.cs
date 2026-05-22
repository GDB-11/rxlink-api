using Application.Core.DTOs.Lookup.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class LookupErrorMapper : IErrorHttpMapper<LookupError>
{
    public IActionResult MapToHttp(LookupError error) =>
        error switch
        {
            LookupDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al recuperar los datos de catálogo."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
