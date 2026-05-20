using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.Encryption.Request;
using Application.Core.DTOs.Encryption.Response;
using Application.Core.Interfaces.Auth;
using BindSharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class EncryptionController : FunctionalController
{
    private readonly IPassword _passwordService;
    private readonly IErrorHttpMapper<ChaChaEncryptionError> _errorMapper;

    public EncryptionController(
        IPassword passwordService,
        IErrorHttpMapper<ChaChaEncryptionError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _passwordService = passwordService;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Encrypts a plain text password using ChaCha20
    /// </summary>
    /// <param name="request">Plain text password to encrypt</param>
    /// <returns>Encrypted cipher text</returns>
    [AllowAnonymous]
    [HttpPost("hash")]
    [ProducesResponseType(typeof(EncryptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult HashPassword([FromBody] EncryptRequest request) =>
        Execute(
            operation: () => _passwordService.HashPassword(request.PlainText)
                .Map(cipherText => new EncryptResponse { CipherText = cipherText }),
            errorMapper: _errorMapper,
            operationName: nameof(HashPassword)
        );

    /// <summary>
    /// Verifies a plain text password against an existing cipher text
    /// </summary>
    /// <param name="request">Plain text and cipher text to compare</param>
    /// <returns>Whether the plain text matches the cipher text</returns>
    [AllowAnonymous]
    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerifyPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult VerifyPassword([FromBody] VerifyPasswordRequest request) =>
        Execute(
            operation: () => _passwordService.VerifyPassword(request.PlainText, request.CipherText)
                .Map(isMatch => new VerifyPasswordResponse { IsMatch = isMatch }),
            errorMapper: _errorMapper,
            operationName: nameof(VerifyPassword)
        );
}
