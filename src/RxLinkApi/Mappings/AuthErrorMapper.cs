using Application.Core.DTOs.Auth.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class AuthErrorMapper : IErrorHttpMapper<AuthenticationError>
{
    public IActionResult MapToHttp(AuthenticationError error) =>
        error switch
        {
            // 202: the password was accepted but login is not complete until the MFA code is verified.
            MfaRequiredError mfa => new ObjectResult(new
            {
                mfaRequired = true,
                mfaToken = mfa.MfaToken,
                expiresAt = mfa.ExpiresAt
            })
            {
                StatusCode = StatusCodes.Status202Accepted
            },

            AccountLockedError locked => LockedResult(locked),

            SessionExpiredError => new UnauthorizedObjectResult(new
            {
                message = error.Message
            }),

            InvalidMfaTokenError or InvalidMfaCodeError => new UnauthorizedObjectResult(new
            {
                message = error.Message
            }),

            UserNotFoundError => new UnauthorizedObjectResult(new
            {
                message = error.Message
            }),

            IncorrectPasswordError => new UnauthorizedObjectResult(new
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
                message = jwt.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            JwtStorageError storage => new ObjectResult(new
            {
                message = storage.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            StoreRefreshTokenError store => new ObjectResult(new
            {
                message = store.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetByUsernameAsyncDomainError email => new ObjectResult(new
            {
                message = email.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            GetByRefreshTokenAsyncDomainError refresh => new ObjectResult(new
            {
                message = refresh.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            ChaChaDecryptError decrypt => new ObjectResult(new
            {
                message = decrypt.Message
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new
            {
                message = "Ocurrió un error inesperado."
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };

    private static ObjectResult LockedResult(AccountLockedError locked) =>
        new(new { message = locked.Message, retryAfterSeconds = locked.RetryAfterSeconds })
        {
            StatusCode = StatusCodes.Status429TooManyRequests
        };
}
