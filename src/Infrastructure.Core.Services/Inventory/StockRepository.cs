using System.Data;
using BindSharp;
using Dapper;
using Infrastructure.Core.DTOs.Inventory;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Inventory;
using Infrastructure.Core.Models.Inventory;

namespace Infrastructure.Core.Services.Inventory;

public sealed class StockRepository : BaseDatabaseService, IStockRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public StockRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<StockMovementPageRow, StockRepositoryError>> GetMovementsAsync(
        Guid medicationCode, int page, int pageSize) =>
        await Result.TryAsync(
            operation: async () =>
            {
                var args = new { MedicationCode = medicationCode, PageSize = pageSize, Offset = (page - 1) * pageSize };
                int total = await _connection.ExecuteScalarAsync<int>(StockRepositorySql.CountMovements, args);
                var items = (await _connection.QueryAsync<StockMovementRow>(StockRepositorySql.GetMovements, args)).ToList();
                return new StockMovementPageRow { Items = items, TotalCount = total };
            },
            errorFactory: StockRepositoryError (ex) => new GetStockError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<StockReportRow>, StockRepositoryError>> GetReportAsync(
        DateTimeOffset from, DateTimeOffset toExclusive) =>
        await Result.TryAsync(
            operation: async () => (IReadOnlyList<StockReportRow>)(await _connection.QueryAsync<StockReportRow>(
                StockRepositorySql.GetReport, new { From = from, ToExclusive = toExclusive })).ToList(),
            errorFactory: StockRepositoryError (ex) => new GetStockError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<StockMovementRow, StockRepositoryError>> RegisterMovementAsync(
        Guid medicationCode, string movementType, short direction, decimal quantity,
        string? batchNumber, DateTime? expiresOn, string? reason, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: () => ExecuteAuditedWriteAsync(
                _connection,
                _userContext,
                async transaction =>
                {
                    int clinicId = await _connection.ExecuteScalarAsync<int>(
                        StockRepositorySql.GetDefaultClinicId, transaction: transaction);
                    var key = new { ClinicId = clinicId, MedicationCode = medicationCode };

                    await _connection.ExecuteAsync(StockRepositorySql.EnsureClinicStock, key, transaction);
                    decimal balance = await _connection.ExecuteScalarAsync<decimal>(
                        StockRepositorySql.LockClinicStock, key, transaction);

                    // Throwing rolls the transaction back; errorFactory translates it.
                    if (direction < 0 && balance < quantity)
                        throw new StockBelowZeroException(balance);

                    await _connection.ExecuteAsync(
                        StockRepositorySql.ApplyToClinicStock,
                        new { key.ClinicId, key.MedicationCode, Delta = direction * quantity, PerformedByUserCode = performedByUserCode },
                        transaction);

                    return await _connection.QuerySingleAsync<StockMovementRow>(
                        StockRepositorySql.InsertMovement,
                        new
                        {
                            key.ClinicId, key.MedicationCode, MovementType = movementType, Direction = direction,
                            Quantity = quantity, BatchNumber = batchNumber, ExpiresOn = expiresOn, Reason = reason,
                            PerformedByUserCode = performedByUserCode
                        },
                        transaction);
                },
                performedByUserCode),
            errorFactory: StockRepositoryError (ex) => ex switch
            {
                StockBelowZeroException below => new StockBelowZeroError(below.Balance),
                _ => new RegisterStockMovementError(ex.Message, ex)
            });

    private sealed class StockBelowZeroException(decimal balance) : Exception
    {
        public decimal Balance { get; } = balance;
    }
}
