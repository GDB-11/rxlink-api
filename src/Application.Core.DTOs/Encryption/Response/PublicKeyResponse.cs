namespace Application.Core.DTOs.Encryption.Response;

public sealed record PublicKeyResponse
{
    public required Guid KeyPairId { get; init; }
    public required string PublicKey { get; init; }
}