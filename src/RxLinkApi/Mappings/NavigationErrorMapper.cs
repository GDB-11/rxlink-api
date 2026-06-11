using Application.Core.DTOs.Navigation.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class NavigationErrorMapper : IErrorHttpMapper<NavigationError>
{
    public IActionResult MapToHttp(NavigationError error) =>
        error switch
        {
            InvalidRoleClaimError => new UnauthorizedObjectResult(new
            {
                message = error.Message
            }),

            NavigationDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message,
                details = dataError.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la navegación."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}