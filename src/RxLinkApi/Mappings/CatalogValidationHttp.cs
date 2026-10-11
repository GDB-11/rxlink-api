using Application.Core.DTOs.Catalog.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

/// <summary>Shared HTTP mapping for catalog write-validation failures, used by every domain mapper that validates catalog codes.</summary>
internal static class CatalogValidationHttp
{
    internal static IActionResult Map(CatalogValidationError error) =>
        error switch
        {
            CatalogItemNotFoundError or CatalogItemInactiveError or CatalogItemNotEnabledError =>
                new UnprocessableEntityObjectResult(new { message = error.Message }),

            CatalogValidationUnavailableError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            },

            _ => new ObjectResult(new { message = "Error inesperado al validar el catálogo." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
