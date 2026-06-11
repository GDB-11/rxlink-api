using Application.Core.DTOs.Encryption.Errors;
using Microsoft.AspNetCore.Mvc;

namespace RxLinkApi.Mappings;

/// <summary>
/// Default implementation for ChaCha encryption errors
/// </summary>
public sealed class ChaChaEncryptionErrorMapper : IErrorHttpMapper<ChaChaEncryptionError>
{
    public IActionResult MapToHttp(ChaChaEncryptionError error) =>
        error switch
        {
            GetBytesError bytesError => new BadRequestObjectResult(new
            {
                message = bytesError.Message,
                details = bytesError.Details
            }),

            GetBytesFromBase64StringError base64Error => new BadRequestObjectResult(new
            {
                message = base64Error.Message,
                details = base64Error.Details
            }),

            ChaChaEncryptError encryptError => new BadRequestObjectResult(new
            {
                message = encryptError.Message,
                details = encryptError.Details
            }),

            ChaChaDecryptError decryptError => new BadRequestObjectResult(new
            {
                message = decryptError.Message,
                details = decryptError.Details
            }),

            PerformDecryption decryptionError => new UnprocessableEntityObjectResult(new
            {
                message = decryptionError.Message,
                details = decryptionError.Details
            }),

            ExtractEncryptedPartsError extractError => new BadRequestObjectResult(new
            {
                message = extractError.Message,
                details = extractError.Details
            }),

            _ => new ObjectResult(new
            {
                message = "An unexpected encryption error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}