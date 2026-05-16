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
                message = error.Message
            }),

            RefreshTokenNotFoundError => new UnauthorizedObjectResult(new
            {
                message = error.Message
            }),

            InvalidUserTokenError => new UnauthorizedObjectResult(new
            {
                message = error.Message
            }),

            UserInactiveError => new UnauthorizedObjectResult(new
            {
                message = error.Message
            }),

            JwtGenerationError jwt => new ObjectResult(new
            {
                message = jwt.Message,
                details = jwt.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            JwtStorageError storage => new ObjectResult(new
            {
                message = storage.Message,
                details = storage.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            StoreRefreshTokenError store => new ObjectResult(new
            {
                message = store.Message,
                details = store.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetByUsernameAsyncDomainError email => new ObjectResult(new
            {
                message = email.Message,
                details = email.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetByRefreshTokenAsyncDomainError refresh => new ObjectResult(new
            {
                message = refresh.Message,
                details = refresh.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            ChaChaDecryptError decrypt => new ObjectResult(new
            {
                message = decrypt.Message,
                details = decrypt.Details
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "An unexpected error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}