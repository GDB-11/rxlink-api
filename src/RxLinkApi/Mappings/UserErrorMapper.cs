using Application.Core.DTOs.User.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class UserErrorMapper : IErrorHttpMapper<UserError>
{
    public IActionResult MapToHttp(UserError error) =>
        error switch
        {
            UserNotFoundError => new NotFoundObjectResult(new
            {
                message = error.Message
            }),

            UserPersonNotFoundError => new BadRequestObjectResult(new
            {
                message = error.Message
            }),

            UserRoleNotFoundError => new BadRequestObjectResult(new
            {
                message = error.Message
            }),

            UserPasswordError => new ObjectResult(new
            {
                message = error.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            UserDataAccessError dataError => new ObjectResult(new
            {
                message = dataError.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Error inesperado al procesar la solicitud de usuarios."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}