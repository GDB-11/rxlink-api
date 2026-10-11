using System.Data;
using BindSharp;
using Dapper;
using Infrastructure.Core.Interfaces.Audit;

namespace Infrastructure.Core.Services;

public abstract class BaseDatabaseService
{
    protected static Result<Unit, TError> ValidateAffectedRows<TError>(
        int affectedRows,
        Func<string, TError> errorFactory,
        string errorMessage) =>
        affectedRows > 0
            ? Result<Unit, TError>.Success(Unit.Value)
            : Result<Unit, TError>.Failure(errorFactory(errorMessage));

    protected static async Task<int> ExecuteNonQueryAsync<TIn>(IDbConnection connection, string sql, TIn entity,
        IDbTransaction? transaction = null) =>
        await connection.ExecuteAsync(sql, entity, transaction);

    protected static async Task<TOut?> ExecuteFirstOrDefaultAsync<TIn, TOut>(IDbConnection connection, string sql,
        TIn entity, IDbTransaction? transaction = null) =>
        await connection.QueryFirstOrDefaultAsync<TOut?>(sql, entity, transaction);

    protected static async Task<TOut?> ExecuteSingleOrDefaultAsync<TIn, TOut>(IDbConnection connection, string sql,
        TIn entity, IDbTransaction? transaction = null) =>
        await connection.QuerySingleOrDefaultAsync<TOut?>(sql, entity, transaction);

    protected static async Task<TOut?>
        ExecuteScalarAsync<TIn, TOut>(IDbConnection connection, string sql, TIn entity) =>
        await connection.ExecuteScalarAsync<TOut?>(sql, entity);

    protected static async Task<IEnumerable<TOut>> ExecuteQueryAsync<TOut>(IDbConnection connection, string sql) =>
        await connection.QueryAsync<TOut>(sql);

    protected static async Task<IEnumerable<TOut>> ExecuteQueryAsync<TIn, TOut>(IDbConnection connection, string sql,
        TIn entity) =>
        await connection.QueryAsync<TOut>(sql, entity);

    protected static async Task<TOut> ExecuteWithTransactionAsync<TOut>(
        IDbConnection connection,
        Func<IDbTransaction, Task<TOut>> operation)
    {
        if (connection.State != ConnectionState.Open)
            connection.Open();

        using IDbTransaction transaction = connection.BeginTransaction();
        try
        {
            TOut result = await operation(transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Stamps the caller's identity into transaction-local settings (<c>app.user_code</c>, <c>app.ip</c>,
    /// <c>app.request_id</c>) that the row-level audit triggers read with <c>current_setting(..., true)</c>.
    /// The settings are transaction-local (<c>set_config(..., true)</c>) so a pooled connection cannot leak
    /// them between requests. Must be the first statement of any transaction that writes to an audited table.
    /// </summary>
    protected static Task SetAuditContextAsync(
        IDbConnection connection,
        IUserContext userContext,
        IDbTransaction transaction,
        Guid? actorOverride = null) =>
        connection.ExecuteAsync(
            """
            SELECT set_config('app.user_code', @UserCode,  true),
                   set_config('app.ip',         @Ip,        true),
                   set_config('app.request_id', @RequestId, true)
            """,
            new
            {
                UserCode = (actorOverride ?? userContext.UserCode)?.ToString() ?? string.Empty,
                Ip = userContext.IpAddress ?? string.Empty,
                RequestId = userContext.RequestId ?? string.Empty
            },
            transaction);

    /// <summary>
    /// Runs <paramref name="operation"/> inside a transaction whose first statement sets the audit context.
    /// Every write to an audited table must go through this helper (or call
    /// <see cref="SetAuditContextAsync"/> itself when it manages its own transaction).
    /// </summary>
    protected static Task<TOut> ExecuteAuditedWriteAsync<TOut>(
        IDbConnection connection,
        IUserContext userContext,
        Func<IDbTransaction, Task<TOut>> operation,
        Guid? actorOverride = null) =>
        ExecuteWithTransactionAsync(connection, async transaction =>
        {
            await SetAuditContextAsync(connection, userContext, transaction, actorOverride);
            return await operation(transaction);
        });

    protected static Task<int> ExecuteAuditedNonQueryAsync<TIn>(
        IDbConnection connection, IUserContext userContext, string sql, TIn entity) =>
        ExecuteAuditedWriteAsync(connection, userContext,
            transaction => ExecuteNonQueryAsync(connection, sql, entity, transaction));

    protected static Task<TOut?> ExecuteAuditedFirstOrDefaultAsync<TIn, TOut>(
        IDbConnection connection, IUserContext userContext, string sql, TIn entity) =>
        ExecuteAuditedWriteAsync(connection, userContext,
            transaction => ExecuteFirstOrDefaultAsync<TIn, TOut>(connection, sql, entity, transaction));

    protected static Task<TOut?> ExecuteAuditedSingleOrDefaultAsync<TIn, TOut>(
        IDbConnection connection, IUserContext userContext, string sql, TIn entity) =>
        ExecuteAuditedWriteAsync(connection, userContext,
            transaction => ExecuteSingleOrDefaultAsync<TIn, TOut>(connection, sql, entity, transaction));
}
