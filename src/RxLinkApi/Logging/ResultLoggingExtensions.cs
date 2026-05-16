using BindSharp;
using BindSharp.Extensions;

namespace RxLinkApi.Logging;

/// <summary>
/// Functional extensions for logging Results as side effects
/// </summary>
public static class ResultLoggingExtensions
{
    /// <summary>
    /// Logs the result using the Tap pattern (side effect at the edge)
    /// </summary>
    public static Result<T, TError> LogResult<T, TError>(
        this Result<T, TError> result,
        IResultLogger logger,
        string operation) =>
        result
            .Do(
                onSuccess: value => logger.LogSuccess(operation, value),
                onFailure: error => logger.LogError(operation, error)
                );

    /// <summary>
    /// Asynchronously logs the result using the TapAsync pattern
    /// </summary>
    public static async Task<Result<T, TError>> LogResultAsync<T, TError>(
        this Task<Result<T, TError>> resultTask,
        IResultLogger logger,
        string operation)
    {
        Result<T, TError> result = await resultTask;
    
        if (result.IsSuccess)
            await logger.LogSuccessAsync(operation, result.Value);
        else
            await logger.LogErrorAsync(operation, result.Error);
    
        return result;
    }

    /// <summary>
    /// Extended Tap that accepts both success and failure actions
    /// </summary>
    private static Result<T, TError> Tap<T, TError>(
        this Result<T, TError> result,
        Action<T> onSuccess,
        Action<TError> onFailure)
    {
        if (result.IsSuccess)
            onSuccess(result.Value);
        else
            onFailure(result.Error);

        return result;
    }
}