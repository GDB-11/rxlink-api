namespace Application.Core.DTOs.Encryption.Response;

public sealed record EncryptResponse
{
    public required string CipherText { get; init; }
}