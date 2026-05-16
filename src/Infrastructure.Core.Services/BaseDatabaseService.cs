using System.Data;
using BindSharp;
using Dapper;

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
    
    protected static async Task<int> ExecuteNonQueryAsync<TIn>(IDbConnection connection, string sql, TIn entity) => 
        await connection.ExecuteAsync(sql, entity);

    protected static async Task<TOut?> ExecuteFirstOrDefaultAsync<TIn, TOut>(IDbConnection connection, string sql, TIn entity) => 
        await connection.QueryFirstOrDefaultAsync<TOut>(sql, entity);

    protected static async Task<TOut?> ExecuteSingleOrDefaultAsync<TIn, TOut>(IDbConnection connection, string sql, TIn entity) => 
        await connection.QuerySingleOrDefaultAsync<TOut>(sql, entity);
    
    protected static async Task<TOut?> ExecuteScalarAsync<TIn, TOut>(IDbConnection connection, string sql, TIn entity) => 
        await connection.ExecuteScalarAsync<TOut>(sql, entity);

    protected static async Task<IEnumerable<TOut>> ExecuteQueryAsync<TOut>(IDbConnection connection, string sql) => 
        await connection.QueryAsync<TOut>(sql);

    protected static async Task<IEnumerable<TOut>> ExecuteQueryAsync<TIn, TOut>(IDbConnection connection, string sql, TIn entity) => 
        await connection.QueryAsync<TOut>(sql, entity);
}