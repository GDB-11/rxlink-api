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
                Type = "EncodingFailed",
                Message = bytesError.Message,
                Detail = bytesError.Details
            }),
            
            GetBytesFromBase64StringError base64Error => new BadRequestObjectResult(new
            {
                Type = "Base64DecodingFailed",
                Message = base64Error.Message,
                Detail = base64Error.Details
            }),
            
            ChaChaEncryptError encryptError => new BadRequestObjectResult(new
            {
                Type = "EncryptionFailed",
                Message = encryptError.Message,
                Detail = encryptError.Details
            }),
            
            ChaChaDecryptError decryptError => new BadRequestObjectResult(new
            {
                Type = "DecryptionFailed",
                Message = decryptError.Message,
                Detail = decryptError.Details
            }),
            
            PerformDecryption decryptionError => new UnprocessableEntityObjectResult(new
            {
                Type = "DecryptionFailed",
                Message = decryptionError.Message,
                Detail = decryptionError.Details
            }),
            
            ExtractEncryptedPartsError extractError => new BadRequestObjectResult(new
            {
                Type = "InvalidEncryptedDataFormat",
                Message = extractError.Message,
                Detail = extractError.Details
            }),
            
            _ => new ObjectResult(new
            {
                Type = "InternalServerError",
                Message = "An unexpected encryption error occurred"
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            }
        };
}