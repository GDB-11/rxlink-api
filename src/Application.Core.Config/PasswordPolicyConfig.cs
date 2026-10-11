namespace Application.Core.Config;

/// <summary>Password rules for staff accounts (NIST 800-63B style: length + breached-password check, no composition or rotation rules).</summary>
public sealed record PasswordPolicyConfig
{
    public int MinLength { get; init; } = 12;
    public int MaxLength { get; init; } = 128;

    /// <summary>Reject passwords found in known breaches (Have I Been Pwned k-anonymity range API).</summary>
    public bool BreachCheckEnabled { get; init; } = true;
}
