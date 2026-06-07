using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.DocumentType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public sealed class DocumentTypeController : FunctionalController
{
    private readonly IDocumentType _documentTypeService;
    private readonly IErrorHttpMapper<LookupError> _errorMapper;

    public DocumentTypeController(
        IDocumentType documentTypeService,
        IErrorHttpMapper<LookupError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _documentTypeService = documentTypeService;
        _errorMapper         = errorMapper;
    }

    /// <summary>
    /// Returns all active document types in the catalog.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GuidLookupItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAllActive() =>
        ExecuteAsync(
            operation:     () => _documentTypeService.GetAllActiveAsync(),
            errorMapper:   _errorMapper,
            operationName: nameof(GetAllActive)
        );
}
