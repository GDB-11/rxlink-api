using BindSharp;

namespace Application.Core.Interfaces.Auth;

public interface IPasswordPolicy
{
    /// <summary>Checks a new password. The failure value is a user-facing reason.</summary>
    Task<Result<Unit, string>> ValidateAsync(string password, string? username = null);
}
