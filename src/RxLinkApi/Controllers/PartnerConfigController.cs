using Application.Core.DTOs.PartnerConfig.Errors;
using Application.Core.DTOs.PartnerConfig.Response;
using Application.Core.Interfaces.PartnerConfig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

/// <summary>Public, PHI-free identity of this deployment (name, logo, brand colours, locale), read from the registry.</summary>
[AllowAnonymous]
[ApiController]
[Route("api/partner")]
public sealed class PartnerConfigController : FunctionalController
{
    private readonly IPartnerConfig _service;
    private readonly IErrorHttpMapper<PartnerConfigError> _errorMapper;

    public PartnerConfigController(
        IPartnerConfig service,
        IErrorHttpMapper<PartnerConfigError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _service = service;
        _errorMapper = errorMapper;
    }

    /// <summary>Returns the partner's display name, logo URL, brand colours, locale/currency/timezone and clinic name.</summary>
    [HttpGet("config/public")]
    [ProducesResponseType(typeof(PublicPartnerConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPublic() =>
        ExecuteAsync(
            operation: () => _service.GetPublicAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetPublic)
        );

    /// <summary>Returns the partner's logo image. Cacheable: clients get the hash from the config's <c>logoUrl</c>.</summary>
    [HttpGet("logo")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetLogo() =>
        ExecuteAsync(
            operation: () => _service.GetLogoAsync(),
            errorMapper: _errorMapper,
            operationName: nameof(GetLogo),
            successMapper: ToLogoResult
        );

    private IActionResult ToLogoResult(PartnerLogoResponse logo)
    {
        string etag = $"\"{logo.Sha256}\"";
        Response.Headers[HeaderNames.CacheControl] = "public, max-age=86400";

        // Content hash as the entity tag lets a returning browser skip the download after the first visit.
        return Request.Headers.IfNoneMatch.Contains(etag)
            ? new StatusCodeResult(StatusCodes.Status304NotModified)
            : File(logo.Data, logo.ContentType, lastModified: null, new Microsoft.Net.Http.Headers.EntityTagHeaderValue(etag));
    }
}
