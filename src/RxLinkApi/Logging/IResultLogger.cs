namespace RxLinkApi.Logging;

/// <summary>
/// Abstraction for logging operations
/// </summary>
public interface IResultLogger
{
    void LogSuccess<T>(string operation, T value);
    void LogError<TError>(string operation, TError error);
    Task LogSuccessAsync<T>(string operation, T value);
    Task LogErrorAsync<TError>(string operation, TError error);
}