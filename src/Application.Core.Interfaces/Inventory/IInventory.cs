using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Catalog.Response;
using Application.Core.DTOs.Inventory.Errors;
using Application.Core.DTOs.Inventory.Request;
using Application.Core.DTOs.Inventory.Response;
using BindSharp;

namespace Application.Core.Interfaces.Inventory;

public interface IInventory
{
    /// <summary>
    /// Enabled medications whose balance in the default clinic is at or below their reorder level
    /// (only those with a reorder level set), emptiest first.
    /// </summary>
    Task<Result<IReadOnlyList<LowStockItemResponse>, CatalogReadError>> GetLowStockAsync();

    /// <summary>Enabled medications with their balance, optionally only the low/out ones and/or matching a name search.</summary>
    Task<Result<CatalogPageResponse<InventoryItemResponse>, InventoryError>> GetPageAsync(
        int page, int pageSize, string? search, bool lowStockOnly);

    /// <summary>Ledger of one enabled medication, newest first.</summary>
    Task<Result<CatalogPageResponse<StockMovementResponse>, InventoryError>> GetMovementsAsync(
        Guid medicationCode, int page, int pageSize);

    /// <summary>Appends an <c>Entry</c>, <c>Adjustment</c> or <c>Loss</c> and updates the balance atomically.</summary>
    Task<Result<StockMovementResponse, InventoryError>> RegisterMovementAsync(
        Guid medicationCode, RegisterStockMovementRequest request);

    /// <summary>Movement totals per medication over an inclusive UTC day range.</summary>
    Task<Result<IReadOnlyList<StockReportItemResponse>, InventoryError>> GetReportAsync(StockReportRequest request);
}
