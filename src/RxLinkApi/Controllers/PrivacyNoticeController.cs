using Application.Core.DTOs.PrivacyNotice.Errors;
using Application.Core.DTOs.PrivacyNotice.Request;
using Application.Core.DTOs.PrivacyNotice.Response;
using Application.Core.Interfaces.PrivacyNotice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

/// <summary>Versions of the partner's Notice of Privacy Practices.</summary>
[ApiController]
[Route("api/privacy-notice")]
public sealed class PrivacyNoticeController : FunctionalController
{
    private readonly IPrivacyNotice _privacyNotice;
    private readonly IErrorHttpMapper<PrivacyNoticeError> _errorMapper;

    public PrivacyNoticeController(
        IPrivacyNotice privacyNotice, IErrorHttpMapper<PrivacyNoticeError> errorMapper, IResultLogger logger)
        : base(logger)
    {
        _privacyNotice = privacyNotice;
        _errorMapper = errorMapper;
    }

    /// <summary>The notice in force today. Any signed-in user (staff or patient).</summary>
    [HttpGet("current")]
    [Authorize]
    [ProducesResponseType(typeof(PrivacyNoticeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetCurrent() =>
        ExecuteAsync(
            operation: () => _privacyNotice.GetCurrentAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetCurrent)
        );

    /// <summary>All published versions, newest first.</summary>
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(PrivacyNoticePageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetVersions([FromQuery] PrivacyNoticePageRequest request) =>
        ExecuteAsync(
            operation: () => _privacyNotice.GetPageAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetVersions)
        );

    /// <summary>Publishes a new version. Published versions are never edited.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(PrivacyNoticeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Publish([FromBody] PublishPrivacyNoticeRequest request) =>
        ExecuteAuthenticatedAsync(
            operation: userCode => _privacyNotice.PublishAsync(request, userCode),
            errorMapper: _errorMapper,
            operationName: nameof(Publish),
            successMapper: notice => Created($"api/privacy-notice/{notice.PrivacyNoticeCode}", notice)
        );
}
