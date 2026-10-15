using Application.Core.DTOs.Inventory.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class InventoryErrorMapper : IErrorHttpMapper<InventoryError>
{
    public IActionResult MapToHttp(InventoryError error) =>
        error switch
        {
            InventoryMedicationNotFoundError => new NotFoundObjectResult(new { message = error.Message }),

            InventoryInvalidRequestError => new BadRequestObjectResult(new { message = error.Message }),

            InventoryInsufficientStockError => new ConflictObjectResult(new
            {
                message = error.Message,
                code = "insufficient_stock"
            }),

            InventoryUnavailableError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            },

            InventoryDataAccessError => new ObjectResult(new { message = error.Message })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new { message = "Error inesperado al procesar el inventario." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
