using Application.Core.DTOs.Catalog.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class CatalogReadErrorMapper : IErrorHttpMapper<CatalogReadError>
{
    public IActionResult MapToHttp(CatalogReadError error) =>
        error switch
        {
            CatalogReadUnavailableError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            },

            CatalogReadDataAccessError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new { message = "Error inesperado al recuperar el catálogo." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
