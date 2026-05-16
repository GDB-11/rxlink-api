namespace Application.Core.DTOs.Encryption.Request;

public sealed record EncryptRequest
{
    public required string PlainText { get; init; }
}