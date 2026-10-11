namespace Application.Core.DTOs.Mfa.Response;

public sealed class MfaEnrollmentResponse
{
    /// <summary>Base32 secret, for manual entry in an authenticator app.</summary>
    public required string Secret { get; init; }

    /// <summary><c>otpauth://</c> URI to render as a QR code.</summary>
    public required string OtpauthUri { get; init; }
}
