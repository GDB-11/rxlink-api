namespace Application.Core.DTOs.Encryption.Request;

public sealed record DecryptRequest
{
    public required string CipherText { get; init; }
}