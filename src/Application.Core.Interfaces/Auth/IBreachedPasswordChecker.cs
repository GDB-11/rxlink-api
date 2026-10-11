namespace Application.Core.Interfaces.Auth;

public interface IBreachedPasswordChecker
{
    /// <summary>
    /// True when the password appears in a known breach corpus. Implementations must fail open
    /// (return false) when the lookup is unavailable, so an outage never blocks account management.
    /// </summary>
    Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default);
}
