namespace Application.Core.Interfaces.Auth;

/// <summary>RFC 6238 time-based one-time passwords (HMAC-SHA1, 6 digits, 30 s step).</summary>
public interface ITotp
{
    /// <summary>Generates a random 160-bit secret, base32 encoded.</summary>
    string GenerateSecret();

    /// <summary>Builds the <c>otpauth://totp</c> URI that authenticator apps import from a QR code.</summary>
    string BuildOtpauthUri(string secret, string issuer, string accountName);

    /// <summary>
    /// Checks <paramref name="code"/> against the current step and one step either side (clock drift).
    /// Returns the matching step so the caller can reject replays, or <c>null</c> when the code is wrong.
    /// </summary>
    long? Validate(string secret, string code);
}
