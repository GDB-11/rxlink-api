using Application.Core.DTOs.PatientAuth.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

public sealed class PatientAuthErrorMapper : IErrorHttpMapper<PatientAuthError>
{
    public IActionResult MapToHttp(PatientAuthError error) =>
        error switch
        {
            PatientNotFoundError or
            PatientIncorrectPasswordError or
            PatientNoCredentialsError => new UnauthorizedObjectResult(new { message = error.Message }),

            PatientInactiveError => new UnauthorizedObjectResult(new { message = error.Message }),

            PatientRefreshTokenNotFoundError => new UnauthorizedObjectResult(new { message = error.Message }),

            PatientAlreadyRegisteredError => new ConflictObjectResult(new { message = error.Message }),

            PersonNotFoundError => new NotFoundObjectResult(new { message = error.Message }),

            PatientJwtGenerationError e => new ObjectResult(new { message = e.Message, details = e.Details })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            PatientJwtStorageError e => new ObjectResult(new { message = e.Message, details = e.Details })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            PatientPasswordHashError e => new ObjectResult(new { message = e.Message, details = e.Details })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            PatientRepositoryError e => new ObjectResult(new { message = e.Message, details = e.Details })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            },

            _ => new ObjectResult(new { message = "Ocurrió un error inesperado." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}
