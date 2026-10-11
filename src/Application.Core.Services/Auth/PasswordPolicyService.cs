using Application.Core.Config;
using Application.Core.Interfaces.Auth;
using BindSharp;

namespace Application.Core.Services.Auth;

public sealed class PasswordPolicyService : IPasswordPolicy
{
    private readonly PasswordPolicyConfig _config;
    private readonly IBreachedPasswordChecker _breachedChecker;

    public PasswordPolicyService(PasswordPolicyConfig config, IBreachedPasswordChecker breachedChecker)
    {
        _config = config;
        _breachedChecker = breachedChecker;
    }

    public async Task<Result<Unit, string>> ValidateAsync(string password, string? username = null)
    {
        if (password.Length < _config.MinLength)
            return Fail($"La contraseña debe tener al menos {_config.MinLength} caracteres.");

        if (password.Length > _config.MaxLength)
            return Fail($"La contraseña no debe superar los {_config.MaxLength} caracteres.");

        if (!string.IsNullOrWhiteSpace(username)
            && password.Contains(username, StringComparison.OrdinalIgnoreCase))
            return Fail("La contraseña no debe contener el nombre de usuario.");

        if (_config.BreachCheckEnabled && await _breachedChecker.IsBreachedAsync(password))
            return Fail("Esta contraseña aparece en filtraciones de datos conocidas. Elija otra.");

        return Result<Unit, string>.Success(Unit.Value);
    }

    private static Result<Unit, string> Fail(string reason) => Result<Unit, string>.Failure(reason);
}
