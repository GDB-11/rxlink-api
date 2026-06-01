using Application.Core.DTOs.Role.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class RoleErrorMapper : IErrorHttpMapper<RoleError>
{
    public IActionResult MapToHttp(RoleError error) =>
        error switch
        {
            RoleNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            RoleDuplicateNameError => new BadRequestObjectResult(new
            {
                message = error.Message
            }),

            RoleDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de roles."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}