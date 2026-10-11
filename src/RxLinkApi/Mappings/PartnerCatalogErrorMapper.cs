using Application.Core.DTOs.PartnerCatalog.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class PartnerCatalogErrorMapper : IErrorHttpMapper<PartnerCatalogError>
{
    public IActionResult MapToHttp(PartnerCatalogError error) =>
        error switch
        {
            PartnerCatalogItemRejectedError rejected => CatalogValidationHttp.Map(rejected.Reason),

            PartnerCatalogRuleError => new UnprocessableEntityObjectResult(new { message = error.Message }),

            PartnerCatalogDataAccessError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new { message = "Error inesperado al procesar la configuración del centro." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
