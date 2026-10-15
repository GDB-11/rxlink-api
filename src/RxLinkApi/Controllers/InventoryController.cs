using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Catalog.Response;
using Application.Core.DTOs.Inventory.Errors;
using Application.Core.DTOs.Inventory.Request;
using Application.Core.DTOs.Inventory.Response;
using Application.Core.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.Logging;
using RxLinkApi.Mappings;

namespace RxLinkApi.Controllers;

[Authorize(Roles = "Administrador,Enfermero")]
[ApiController]
[Route("api/[controller]")]
public sealed class InventoryController : FunctionalController
{
    private readonly IInventory _inventory;
    private readonly IErrorHttpMapper<CatalogReadError> _readErrorMapper;
    private readonly IErrorHttpMapper<InventoryError> _errorMapper;

    public InventoryController(
        IInventory inventory,
        IErrorHttpMapper<CatalogReadError> readErrorMapper,
        IErrorHttpMapper<InventoryError> errorMapper,
        IResultLogger logger)
        : base(logger)
    {
        _inventory = inventory;
        _readErrorMapper = readErrorMapper;
        _errorMapper = errorMapper;
    }

    /// <summary>Enabled medications at or below their reorder level in the default clinic, emptiest first.</summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IReadOnlyList<LowStockItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetLowStock() =>
        ExecuteAsync(
            operation: () => _inventory.GetLowStockAsync(),
            errorMapper: _readErrorMapper,
            operationName: nameof(GetLowStock)
        );

    /// <summary>Enabled medications with their balance; filter by name or by low/out of stock.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CatalogPageResponse<InventoryItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool lowStockOnly = false) =>
        ExecuteAsync(
            operation: () => _inventory.GetPageAsync(page, pageSize, search, lowStockOnly),
            errorMapper: _errorMapper,
            operationName: nameof(GetPage)
        );

    /// <summary>Stock ledger of one medication, newest first.</summary>
    [HttpGet("{medicationCode:guid}/movements")]
    [ProducesResponseType(typeof(CatalogPageResponse<StockMovementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetMovements(
        Guid medicationCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        ExecuteAsync(
            operation: () => _inventory.GetMovementsAsync(medicationCode, page, pageSize),
            errorMapper: _errorMapper,
            operationName: nameof(GetMovements)
        );

    /// <summary>Registers an entry, adjustment or loss. A decrease larger than the balance is rejected (409).</summary>
    [HttpPost("{medicationCode:guid}/movements")]
    [ProducesResponseType(typeof(StockMovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RegisterMovement(Guid medicationCode, [FromBody] RegisterStockMovementRequest request) =>
        ExecuteAsync(
            operation: () => _inventory.RegisterMovementAsync(medicationCode, request),
            errorMapper: _errorMapper,
            operationName: nameof(RegisterMovement)
        );

    /// <summary>Movement totals per medication over a mandatory inclusive date range.</summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(IReadOnlyList<StockReportItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetReport([FromQuery] StockReportRequest request) =>
        ExecuteAsync(
            operation: () => _inventory.GetReportAsync(request),
            errorMapper: _errorMapper,
            operationName: nameof(GetReport)
        );
}
