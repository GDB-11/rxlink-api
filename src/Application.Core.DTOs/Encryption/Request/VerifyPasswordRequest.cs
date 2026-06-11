namespace Application.Core.DTOs.Encryption.Request;

public sealed record VerifyPasswordRequest
{
    public required string PlainText { get; init; }
    public required string CipherText { get; init; }
}