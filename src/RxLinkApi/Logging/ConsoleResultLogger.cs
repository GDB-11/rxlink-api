namespace RxLinkApi.Logging;

/// <summary>
/// Simulated logger implementation (replace with actual logging library)
/// </summary>
public sealed class ConsoleResultLogger : IResultLogger
{
    public void LogSuccess<T>(string operation, T value)
    {
        // Simulated: Replace with actual logger (Serilog, NLog, etc.)
        Console.WriteLine($"[SUCCESS] {operation}: {value}");
    }

    public void LogError<TError>(string operation, TError error)
    {
        // Simulated: Replace with actual logger
        Console.WriteLine($"[ERROR] {operation}: {error}");
    }

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
}