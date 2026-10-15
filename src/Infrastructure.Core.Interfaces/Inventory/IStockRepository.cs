using BindSharp;
using Infrastructure.Core.DTOs.Inventory;
using Infrastructure.Core.Models.Inventory;

namespace Infrastructure.Core.Interfaces.Inventory;

/// <summary>The default clinic's stock ledger (<c>StockMovement</c>) and its cached balance (<c>ClinicStock</c>).</summary>
public interface IStockRepository
{
    /// <summary>Movements of one medication, newest first.</summary>
    Task<Result<StockMovementPageRow, StockRepositoryError>> GetMovementsAsync(Guid medicationCode, int page, int pageSize);

    /// <summary>Ledger totals per medication for <c>[from, toExclusive)</c>.</summary>
    Task<Result<IReadOnlyList<StockReportRow>, StockRepositoryError>> GetReportAsync(DateTimeOffset from, DateTimeOffset toExclusive);

    /// <summary>
    /// Appends a movement and applies it to the balance in one transaction, locking the balance row.
    /// Fails with <see cref="StockBelowZeroError"/> (nothing written) when a decrease exceeds the balance.
    /// </summary>
    Task<Result<StockMovementRow, StockRepositoryError>> RegisterMovementAsync(
        Guid medicationCode, string movementType, short direction, decimal quantity,
        string? batchNumber, DateTime? expiresOn, string? reason, Guid performedByUserCode);
}
