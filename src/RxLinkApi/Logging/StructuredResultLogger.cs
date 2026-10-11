namespace RxLinkApi.Logging;

/// <summary>
/// Logs only operation names and error type names, never payloads: success values are
/// DTOs that carry PHI/PII, and error messages can embed SQL text or request data.
/// </summary>
public sealed class StructuredResultLogger : IResultLogger
{
    private readonly ILogger<StructuredResultLogger> _logger;

    public StructuredResultLogger(ILogger<StructuredResultLogger> logger)
    {
        _logger = logger;
    }

    public void LogSuccess<T>(string operation, T value) =>
        _logger.LogInformation("Operation {Operation} succeeded", operation);

    public void LogError<TError>(string operation, TError error) =>
        _logger.LogWarning("Operation {Operation} failed with {ErrorType}", operation, Describe(error));

    public Task LogSuccessAsync<T>(string operation, T value)
    {
        LogSuccess(operation, value);
        return Task.CompletedTask;
    }

    public Task LogErrorAsync<TError>(string operation, TError error)
    {
        LogError(operation, error);
        return Task.CompletedTask;
    }

    // Strings are only passed by FunctionalController with fixed messages, so they are safe to log.
    private static string Describe<TError>(TError error) =>
        error is string fixedMessage ? fixedMessage : error?.GetType().Name ?? typeof(TError).Name;
}
