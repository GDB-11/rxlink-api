namespace Infrastructure.Core.Models.Mfa;

public sealed class MfaRow
{
    /// <summary>ChaCha-encrypted base32 TOTP secret.</summary>
    public required string SecretEncrypted { get; init; }
    public required bool IsConfirmed { get; init; }
    public long? LastUsedStep { get; init; }
}
