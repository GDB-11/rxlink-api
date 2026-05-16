using Application.Core.DTOs.Auth.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class AuthErrorMapper : IErrorHttpMapper<AuthenticationError>
{
    public IActionResult MapToHttp(AuthenticationError error) =>
        error switch
        {
            UserNotFoundError => new UnauthorizedObjectResult(new
            {
                error = "Unauthorized",
                message = error.Message
            }),

            RefreshTokenNotFoundError => new UnauthorizedObjectResult(new
            {
                error = "Unauthorized",
                message = error.Message
            }),

            InvalidUserTokenError => new UnauthorizedObjectResult(new
            {
                error = "Unauthorized",
                message = error.Message
            }),

            UserInactiveError => new UnauthorizedObjectResult(new
            {
                error = "Unauthorized",
                message = error.Message
            }),

            JwtGenerationError jwt => new ObjectResult(new
            {
                error = "InternalServerError",
                message = jwt.Message,
                details = jwt.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            JwtStorageError storage => new ObjectResult(new
            {
                error = "InternalServerError",
                message = storage.Message,
                details = storage.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            StoreRefreshTokenError store => new ObjectResult(new
            {
                error = "InternalServerError",
                message = store.Message,
                details = store.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetByUsernameAsyncDomainError email => new ObjectResult(new
            {
                error = "InternalServerError",
                message = email.Message,
                details = email.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetByRefreshTokenAsyncDomainError refresh => new ObjectResult(new
            {
                error = "InternalServerError",
                message = refresh.Message,
                details = refresh.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            ChaChaDecryptError decrypt => new ObjectResult(new
            {
                error = "InternalServerError",
                message = decrypt.Message,
                details = decrypt.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                error = "InternalServerError",
                message = "An unexpected error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}